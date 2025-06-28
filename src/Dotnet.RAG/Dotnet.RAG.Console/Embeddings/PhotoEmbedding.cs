namespace Dotnet.RAG.Console.Embeddings;

public class PhotoEmbedding
{ 
    public string PhotoPath { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;

    public float[] Vector { get; set; } = [];
}