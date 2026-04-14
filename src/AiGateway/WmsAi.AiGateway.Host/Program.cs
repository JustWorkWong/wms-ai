using WmsAi.AiGateway.Host.Endpoints;
using WmsAi.AiGateway.Host.Events;
using WmsAi.AiGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 注册 AiGateway 模块（包含 CAP 事件总线配置）
builder.Services.AddAiGatewayModule(builder.Configuration);

// 注册 CAP 订阅者（必须在 AddCap 之后注册）
builder.Services.AddSingleton<InboundEventConsumer>();

// 注册控制器
builder.Services.AddControllers();

// BusinessApiClient 需要访问当前请求上下文
builder.Services.AddHttpContextAccessor();

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WmsAi.AiGateway API",
        Version = "v1",
        Description = "AI 网关域 API - AI 工作流、智能体、模型管理"
    });
});

var app = builder.Build();

// 启用 Swagger（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AiGateway API v1");
    });
}

app.MapGet("/", () => Results.Ok());
app.MapDefaultEndpoints();

// 映射控制器路由
app.MapControllers();

// 映射 Workflow 恢复 API
app.MapWorkflowEndpoints();

// 初始化数据库
await AiGatewayDatabaseInitializer.InitializeAsync(app.Services);

// 强制实例化 CAP 订阅者，确保 CAP 能发现订阅方法
using (var scope = app.Services.CreateScope())
{
    var _ = scope.ServiceProvider.GetRequiredService<InboundEventConsumer>();
}

app.Run();
