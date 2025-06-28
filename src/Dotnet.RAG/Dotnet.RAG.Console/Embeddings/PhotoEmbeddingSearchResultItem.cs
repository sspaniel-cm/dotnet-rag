namespace Dotnet.RAG.Console.Embeddings;

public class PhotoEmbeddingSearchResultItem
{
    public string PhotoPath { get; set; } = string.Empty;

    public float Distance { get; set; }
}