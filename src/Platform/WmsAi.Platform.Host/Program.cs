using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPlatformModule(builder.Configuration);

var app = builder.Build();

await PlatformDatabaseInitializer.InitializeAsync(app.Services);

app.MapPost("/api/platform/tenants", async (
    CreateTenantCommand command,
    CreateTenantHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(command, cancellationToken);
    return Results.Created($"/api/platform/tenants/{result.TenantCode}", result);
});

app.MapHealthChecks("/health", new HealthCheckOptions());
app.MapDefaultEndpoints();

app.Run();
