using Hangfire;
using WmsAi.Operations.Host;

var builder = WebApplication.CreateBuilder(args);

// Add Nacos configuration (optional)
builder.Configuration.AddNacosConfiguration(builder.Configuration);

builder.AddServiceDefaults();

// Add Operations module
builder.Services.AddOperationsModule(builder.Configuration);

var app = builder.Build();

// Map Hangfire Dashboard
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>() // Dev only - secure in production
});

app.MapGet("/", () => Results.Ok(new
{
    Service = "WmsAi.Operations",
    Status = "Running",
    HangfireDashboard = "/hangfire"
}));

app.MapDefaultEndpoints();

app.Run();
