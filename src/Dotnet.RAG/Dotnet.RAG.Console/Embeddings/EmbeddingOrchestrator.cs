using Azure.AI.Inference;
using Azure.AI.Vision.ImageAnalysis;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Dotnet.RAG.Console.Embeddings;

public class EmbeddingOrchestrator(
    SqlConnection sqlConnection,
    ImageAnalysisClient imageAnalysisClient,
    EmbeddingsClient embeddingsClient)
{
    public const string EmbeddingModel = "embed-v-4-0";
    public const int EmbeddingDimensions = 256;

    public async IAsyncEnumerable<PhotoEmbedding> ProcessAsync(string[] photoPaths)
    {
        var sql = "SELECT PhotoPath FROM DotnetRAG.dbo.PhotoEmbedding WHERE PhotoPath in @photoPaths";
        var existingPhotoPaths = await sqlConnection.QueryAsync<string>(sql, new { photoPaths });
        var photoPathsToEmbed = photoPaths.Except(existingPhotoPaths).ToArray();

        if (photoPathsToEmbed.Length == 0)
        {
            System.Console.Write("All photos embedded");
        }

        foreach (var photoPathToEmbed in photoPathsToEmbed)
        {
            System.Console.WriteLine($"Embedding {Path.GetFileName(photoPathToEmbed)}");

            var photoEmbedding = new PhotoEmbedding { PhotoPath = photoPathToEmbed };
            await GenerateCaptionAsync(photoEmbedding);
            await GenerateVectorAsync(photoEmbedding);
            await StoreAsync(photoEmbedding);
            yield return photoEmbedding;
        }
    }

    private async Task GenerateCaptionAsync(PhotoEmbedding photoEmbedding)
    {
        var photoData = await File.ReadAllBytesAsync(photoEmbedding.PhotoPath);
        var captionResult = await imageAnalysisClient.AnalyzeAsync(new BinaryData(photoData), VisualFeatures.Caption);
        photoEmbedding.Caption = captionResult?.Value?.Caption?.Text ?? string.Empty;
        photoEmbedding.Caption = photoEmbedding.Caption.StartsWith("a ") ? photoEmbedding.Caption[2..] : photoEmbedding.Caption;
    }

    private async Task GenerateVectorAsync(PhotoEmbedding photoEmbedding)
    {
        var options = new EmbeddingsOptions([photoEmbedding.Caption]);
        options.Model = EmbeddingModel;
        options.Dimensions = EmbeddingDimensions;

        var embeddingResult = await embeddingsClient.EmbedAsync(options);
        photoEmbedding.Vector = embeddingResult?.Value?.Data?.FirstOrDefault()?.Embedding?.ToObjectFromJson<float[]>() ?? [];
    }

    private async Task StoreAsync(PhotoEmbedding photoEmbedding)
    {
        var sql = @"
                INSERT INTO DotnetRAG.dbo.PhotoEmbedding (PhotoPath, Caption, Vector) 
                VALUES (@PhotoPath, @Caption, @VectorJson);";

        var parameters = new
        {
            photoEmbedding.PhotoPath,
            photoEmbedding.Caption,
            VectorJson = "[" + string.Join(", ", photoEmbedding.Vector.Select(f => f.ToString("R"))) + "]"
        };

        await sqlConnection.ExecuteAsync(sql, parameters);
    }
}