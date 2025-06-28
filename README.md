# RAG with SQL Server in .NET

## Overview

This project illustrates the development of a custom RAG (Retrieval Augmented Generation) pipeline within a .NET application. It facilitates semantic photo search through the integration of SQL Server 2025, Azure AI Vision, and Azure AI Foundry.

## Prerequisites

- Azure AI Services resource
    - Endpoint
    - API Key
    - https://docs.azure.cn/en-us/ai-services/multi-service-resource
- Azure AI Foundry embedding model
    - Endpoint
    - API Key
    - https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/models-featured#cohere-command-and-embed
- Docker
- SQL Server 2025
- .NET SDK 8.0

## Getting Started

1. Clone the repository.

2. Add your Azure AI Services and Azure AI Foundry credentials to the `program.cs` file:

   ```csharp
    // azure image analysis client
    var imageAnalysisEndpoint = "[Azure AI Vision Endpoint]";
    var imageAnalysisApiKey = "[Azure AI Vision API Key]";
    var imageAnalysisClient = new ImageAnalysisClient(new Uri(imageAnalysisEndpoint), new AzureKeyCredential(imageAnalysisApiKey));

    // azure embeddings client
    var embeddingsEndpoint = "[Azure Foundry Endpoint]";
    var embeddingsApiKey = "[Azure Foundry API Key]";
    var embeddingsClient = new EmbeddingsClient(new Uri(embeddingsEndpoint), new AzureKeyCredential(embeddingsApiKey));
    ```

3. Run docker compose (src/Dotnet.RAG) to start SQL Server:

```bash
   docker-compose up -d
```

4. Run the application (src/Dotnet.RAG/Dotnet.RAG.Console):

```bash
   dotnet run
```

## License

This project is licensed under the MIT License.