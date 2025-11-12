using Azure.Data.Tables;
using CentralMemoryMcp.Functions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Modern builder-based bootstrap using IHostApplicationBuilder
var builder = FunctionsApplication.CreateBuilder(args);

// Services
builder.ConfigureFunctionsWebApplication();

// Register Azure Table Storage
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
        ?? throw new InvalidOperationException("AzureWebJobsStorage connection string is not configured.");
    return new TableServiceClient(connectionString);
});

// Register application services
builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
builder.Services.AddSingleton<IKnowledgeGraphService, KnowledgeGraphService>();
builder.Services.AddSingleton<IRelationService, RelationService>();

builder.Build().Run();
