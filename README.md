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

2. Run the docker compose to set up the SQL Server database:

   ```bash
   docker-compose up -d
   ```

3. Run the application:

   ```bash
   dotnet run
   ```

## License

This project is licensed under the MIT License.