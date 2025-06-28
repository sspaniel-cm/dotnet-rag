using Azure;
using Azure.AI.Inference;
using Azure.AI.Vision.ImageAnalysis;
using Dapper;
using Dotnet.RAG.Console.Embeddings;
using Microsoft.Data.SqlClient;

// azure image analysis client
var imageAnalysisEndpoint = "[Azure AI Vision Endpoint]";
var imageAnalysisApiKey = "[Azure AI Vision API Key]";
var imageAnalysisClient = new ImageAnalysisClient(new Uri(imageAnalysisEndpoint), new AzureKeyCredential(imageAnalysisApiKey));

// azure embeddings client
var embeddingsEndpoint = "[Azure Foundry Endpoint]";
var embeddingsApiKey = "[Azure Foundry API Key]";
var embeddingsClient = new EmbeddingsClient(new Uri(embeddingsEndpoint), new AzureKeyCredential(embeddingsApiKey));

// sql server
var connectionString = "Server=localhost; User Id=SA; Password=password123!; TrustServerCertificate=true;";
var sqlConnection = new SqlConnection(connectionString);
await sqlConnection.OpenAsync();

// initialize database
var sql = @"
    IF DB_ID('DotnetRAG') IS NULL
    BEGIN
        CREATE DATABASE DotnetRAG;
    END";

await sqlConnection.ExecuteAsync(sql);

sql = @"
    USE DotnetRAG;

    IF OBJECT_ID('dbo.PhotoEmbedding', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PhotoEmbedding (
            PhotoEmbeddingId INT IDENTITY(1, 1) PRIMARY KEY,
            PhotoPath VARCHAR(MAX) NOT NULL,
            Caption VARCHAR(MAX) NOT NULL,
            Vector VECTOR(256) NOT NULL);
    END";

await sqlConnection.ExecuteAsync(sql);

// create and store photo embeddings
var photoPaths = Directory.GetFiles("Photos", "*.jpg", SearchOption.AllDirectories).ToArray();

var embeddingOrchestrator = new EmbeddingOrchestrator(sqlConnection, imageAnalysisClient, embeddingsClient);

await foreach (var photoEmbedding in embeddingOrchestrator.ProcessAsync(photoPaths))
{
    Console.WriteLine($"Done {Path.GetFileName(photoEmbedding.PhotoPath)}");
}

// index is not dynamically updated, so we need to recreate it
sql = @"
    DROP INDEX IF EXISTS PhotoEmbeddingIndex ON DotnetRAG.dbo.PhotoEmbedding;

    CREATE VECTOR INDEX PhotoEmbeddingIndex ON DotnetRAG.dbo.PhotoEmbedding(Vector) 
    WITH (METRIC = 'cosine', TYPE = 'diskann');";

await sqlConnection.ExecuteAsync(sql);

// query loop
while (true)
{
    Console.WriteLine();
    Console.WriteLine("-----------------------------------------");
    Console.WriteLine("Enter a search query (or 'exit' to quit):");
    var query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query))
        continue;

    if (query.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var options = new EmbeddingsOptions([query]);
    options.Model = EmbeddingOrchestrator.EmbeddingModel;
    options.Dimensions = EmbeddingOrchestrator.EmbeddingDimensions;

    var embeddingResult = await embeddingsClient.EmbedAsync(options);
    var vector = embeddingResult?.Value?.Data?.FirstOrDefault()?.Embedding?.ToObjectFromJson<float[]>();

    if (vector == null || vector.Length <= 0)
    {
        throw new InvalidOperationException($"Failed to generate embedding for '{query}'.");
    }

    var vectorJson = "[" + string.Join(", ", vector.Select(f => f.ToString("R"))) + "]";

    sql = $@"
        DECLARE @vector VECTOR({EmbeddingOrchestrator.EmbeddingDimensions}) = CAST(@VectorJson AS VECTOR({EmbeddingOrchestrator.EmbeddingDimensions}));
        SELECT 
            photoEmbedding.PhotoPath, searchResults.distance
        FROM
            VECTOR_SEARCH(
                TABLE = DotnetRAG.dbo.PhotoEmbedding AS photoEmbedding, 
                COLUMN = Vector, 
                SIMILAR_TO = @vector, 
                METRIC = 'cosine', 
                TOP_N = {6}
            ) AS searchResults
        ORDER BY searchResults.distance;";

    var searchResults = await sqlConnection.QueryAsync<PhotoEmbeddingSearchResultItem>(sql, new { vectorJson });

    Console.WriteLine();
    Console.WriteLine("Results:");

    foreach (var resultItem in searchResults)
    {
        Console.WriteLine($"{Path.GetFileName(resultItem.PhotoPath)} Score: {resultItem.Distance}");
    }
}

await sqlConnection.DisposeAsync();
