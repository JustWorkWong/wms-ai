using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WmsAi.Platform.Application.Tenants;
using WmsAi.Platform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPlatformModule(builder.Configuration);

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WmsAi.Platform API",
        Version = "v1",
        Description = "平台域 API - 租户、仓库、用户管理"
    });
});

var app = builder.Build();

await PlatformDatabaseInitializer.InitializeAsync(app.Services);

// 启用 Swagger（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Platform API v1");
    });
}

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
