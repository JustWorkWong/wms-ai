using Hangfire;
using WmsAi.Operations.Host;

var builder = WebApplication.CreateBuilder(args);

// Add Nacos configuration (optional)
builder.Configuration.AddNacosConfiguration(builder.Configuration);

builder.AddServiceDefaults();

// Add Operations module
builder.Services.AddOperationsModule(builder.Configuration);

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WmsAi.Operations API",
        Version = "v1",
        Description = "后台任务域 API - Hangfire 任务管理"
    });
});

var app = builder.Build();

// 启用 Swagger（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Operations API v1");
    });
}

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
