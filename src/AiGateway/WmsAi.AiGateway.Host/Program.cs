using WmsAi.AiGateway.Host.Events;
using WmsAi.AiGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add AiGateway module (includes CAP configuration)
builder.Services.AddAiGatewayModule(builder.Configuration);

builder.Services.AddScoped<InboundEventConsumer>();

// Add controllers
builder.Services.AddControllers();

// Add HttpContextAccessor for BusinessApiClient
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapGet("/", () => Results.Ok());
app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();

// Placeholder API endpoints for inspection workflow
app.MapPost("/api/ai/inspections/start", async (StartInspectionRequest request) =>
{
    // TODO: Implement workflow start logic
    return Results.Ok(new { workflowRunId = Guid.NewGuid(), status = "Pending" });
});

app.MapGet("/api/ai/inspections/{id:guid}", async (Guid id) =>
{
    // TODO: Implement workflow status query
    return Results.Ok(new { id, status = "Running", currentNode = "CheckEvidenceCompleteness" });
});

app.MapPost("/api/ai/inspections/{id:guid}/resume", async (Guid id, ResumeInspectionRequest request) =>
{
    // TODO: Implement workflow resume logic
    return Results.Ok(new { id, status = "Resumed" });
});

// Initialize database
await AiGatewayDatabaseInitializer.InitializeAsync(app.Services);

app.Run();

public record StartInspectionRequest(Guid QcTaskId, string TenantId, string WarehouseId, string UserId);
public record ResumeInspectionRequest(string Decision, string Reasoning);

