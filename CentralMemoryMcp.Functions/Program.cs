using Azure.Data.Tables;
using CentralMemoryMcp.Functions.Services;
using CentralMemoryMcp.Functions.Storage;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Modern builder-based bootstrap using IHostApplicationBuilder
var builder = FunctionsApplication.CreateBuilder(args);

// Services
builder.ConfigureFunctionsWebApplication();

// Register application services
builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
builder.Services.AddSingleton<IKnowledgeGraphService, KnowledgeGraphService>();
builder.Services.AddSingleton<IRelationService, RelationService>();

builder.Build().Run();
