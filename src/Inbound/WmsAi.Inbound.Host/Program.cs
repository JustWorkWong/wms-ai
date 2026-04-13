using WmsAi.Inbound.Application.Support;
using WmsAi.Inbound.Host;
using WmsAi.Inbound.Application.Inbound;
using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;
using WmsAi.Inbound.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddInboundModule(builder.Configuration);

var app = builder.Build();

await BusinessDatabaseInitializer.InitializeAsync(app.Services);

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

app.MapPost("/api/inbound/qc/decisions", async (
    FinalizeQcDecisionCommand command,
    FinalizeQcDecisionHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(command, cancellationToken);
    return Results.Created($"/api/inbound/qc/decisions/{result.QcDecisionId}", result);
});

app.MapDefaultEndpoints();

app.Run();
