using WmsAi.AiGateway.Host.Events;
using WmsAi.AiGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 注册 AiGateway 模块（包含 CAP 事件总线配置）
builder.Services.AddAiGatewayModule(builder.Configuration);

builder.Services.AddScoped<InboundEventConsumer>();

// 注册控制器
builder.Services.AddControllers();

// BusinessApiClient 需要访问当前请求上下文
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapGet("/", () => Results.Ok());
app.MapDefaultEndpoints();

// 映射控制器路由
app.MapControllers();

// 初始化数据库
await AiGatewayDatabaseInitializer.InitializeAsync(app.Services);

app.Run();
