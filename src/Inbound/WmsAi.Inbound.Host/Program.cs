using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WmsAi.Inbound.Application.Support;
using WmsAi.Inbound.Host;
using WmsAi.Inbound.Application.Inbound;
using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;
using WmsAi.Inbound.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddInboundModule(builder.Configuration);

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WmsAi.Inbound API",
        Version = "v1",
        Description = "入库域 API - 到货通知、收货、质检"
    });
});

var app = builder.Build();

await BusinessDatabaseInitializer.InitializeAsync(app.Services);

// 启用 Swagger（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inbound API v1");
    });
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception) when (exception is InboundException or ArgumentException)
    {
        var error = InboundHttpExceptionMapper.Map(exception);
        context.Response.StatusCode = error.StatusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            title = error.Title,
            detail = error.Detail
        });
    }
});

app.MapPost("/api/inbound/notices", async (
    CreateInboundNoticeCommand command,
    CreateInboundNoticeHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(command, cancellationToken);
    return Results.Created($"/api/inbound/notices/{result.InboundNoticeId}", result);
});

app.MapPost("/api/inbound/receipts", async (
    RecordReceiptCommand command,
    RecordReceiptHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(command, cancellationToken);
    return Results.Created($"/api/inbound/receipts/{result.ReceiptId}", result);
});

app.MapGet("/api/inbound/qc/tasks", async (
    string tenantId,
    string warehouseId,
    GetQcTasksHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(tenantId, warehouseId, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/inbound/qc/tasks/{qcTaskId:guid}", async (
    Guid qcTaskId,
    HttpContext httpContext,
    GetQcTaskByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var tenantId = httpContext.Request.Query["tenantId"].FirstOrDefault()
        ?? httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    var warehouseId = httpContext.Request.Query["warehouseId"].FirstOrDefault()
        ?? httpContext.Request.Headers["X-Warehouse-Id"].FirstOrDefault();

    if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(warehouseId))
        return Results.BadRequest("tenantId and warehouseId are required");

    var result = await handler.Handle(qcTaskId, tenantId, warehouseId, cancellationToken);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

app.MapGet("/api/inbound/qc/tasks/{qcTaskId:guid}/evidence", async (
    Guid qcTaskId,
    HttpContext httpContext,
    GetQcEvidenceHandler handler,
    CancellationToken cancellationToken) =>
{
    var tenantId = httpContext.Request.Query["tenantId"].FirstOrDefault()
        ?? httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    var warehouseId = httpContext.Request.Query["warehouseId"].FirstOrDefault()
        ?? httpContext.Request.Headers["X-Warehouse-Id"].FirstOrDefault();

    if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(warehouseId))
        return Results.BadRequest("tenantId and warehouseId are required");

    var result = await handler.Handle(qcTaskId, tenantId, warehouseId, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/inbound/skus/{skuCode}/quality-profile", async (
    string skuCode,
    HttpContext httpContext,
    GetSkuQualityProfileHandler handler,
    CancellationToken cancellationToken) =>
{
    var tenantId = httpContext.Request.Query["tenantId"].FirstOrDefault()
        ?? httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

    if (string.IsNullOrEmpty(tenantId))
        return Results.BadRequest("tenantId is required");

    var result = await handler.Handle(skuCode, tenantId, cancellationToken);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

app.MapPost("/api/inbound/qc/decisions", async (
    FinalizeQcDecisionCommand command,
    FinalizeQcDecisionHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(command, cancellationToken);
    return Results.Created($"/api/inbound/qc/decisions/{result.QcDecisionId}", result);
});

app.MapHealthChecks("/health", new HealthCheckOptions());

app.Run();
