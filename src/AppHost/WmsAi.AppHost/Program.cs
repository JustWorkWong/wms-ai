using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WmsAi_Gateway_Host>("gateway");

builder.Build().Run();
