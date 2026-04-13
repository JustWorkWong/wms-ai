using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddContainer("postgres", "postgres:16");
builder.AddContainer("redis", "redis:7");
builder.AddContainer("rabbitmq", "rabbitmq:3-management");
builder.AddContainer("minio", "minio/minio");
builder.AddContainer("nacos", "nacos/nacos-server");

builder.AddProject<Projects.WmsAi_Gateway_Host>("gateway");
builder.AddProject<Projects.WmsAi_Platform_Host>("platform");
builder.AddProject<Projects.WmsAi_Inbound_Host>("inbound");
builder.AddProject<Projects.WmsAi_AiGateway_Host>("ai-gateway");
builder.AddProject<Projects.WmsAi_Operations_Host>("operations");

builder.Build().Run();
