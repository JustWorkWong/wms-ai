using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Functions;
using WmsAi.AiGateway.Application.Services;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Domain.MafSessions;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Agents;
using WmsAi.AiGateway.Infrastructure.Functions;
using WmsAi.AiGateway.Infrastructure.Persistence;
using WmsAi.AiGateway.Infrastructure.Repositories;
using WmsAi.AiGateway.Infrastructure.Services;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.AiGateway.Infrastructure;

public static class AiGatewayModuleExtensions
{
    public static IServiceCollection AddAiGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AiDb")
            ?? "Host=localhost;Database=AiDb;Username=postgres;Password=postgres";

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ")
            ?? "amqp://guest:guest@localhost:5672";

        services.AddSingleton<VersionedEntitySaveChangesInterceptor>();
        services.AddSingleton<DomainEventDispatcher>();
        services.AddSingleton<DomainEventInterceptor>();

        services.AddDbContext<AiDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventInterceptor>());
        });

        // 注册仓储
        services.AddScoped<IMafSessionRepository, MafSessionRepository>();
        services.AddScoped<IMafWorkflowRunRepository, MafWorkflowRunRepository>();
        services.AddScoped<IAiInspectionRunRepository, AiInspectionRunRepository>();
        services.AddScoped<IAiModelProfileRepository, AiModelProfileRepository>();
        services.AddScoped<IAiModelProviderRepository, AiModelProviderRepository>();
        services.AddScoped<IAiRoutingPolicyRepository, AiRoutingPolicyRepository>();

        // 注册领域服务
        services.AddScoped<IMafPersistenceService, MafPersistenceService>();
        services.AddScoped<IModelRoutingService, ModelRoutingService>();
        services.AddScoped<IAgUiEventStreamService, AgUiEventStreamService>();
        services.AddScoped<IAgUiEventTransformer, AgUiEventTransformer>();

        // 注册 IChatClient（使用 Microsoft.Extensions.AI）
        services.AddSingleton<IChatClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();

            var endpoint = configuration["AiProviders:Qwen:Endpoint"]
                ?? throw new InvalidOperationException("AiProviders:Qwen:Endpoint not configured");
            var apiKey = configuration["AiProviders:Qwen:ApiKey"]
                ?? throw new InvalidOperationException("AiProviders:Qwen:ApiKey not configured");
            var modelName = configuration["AiProviders:Qwen:DeploymentName"] ?? "qwen3-1.7b";

            // 创建 OpenAI 兼容客户端
            var openAiClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            });

            var rawClient = openAiClient.GetChatClient(modelName);

            // 包装中间件：OpenTelemetry + Logging
            var chatClient = ((IChatClient)rawClient)
                .AsBuilder()
                .UseOpenTelemetry(
                    sourceName: "WmsAi.AiGateway.AiModel",
                    configure: cfg =>
                    {
                        // 开发环境记录完整 prompt 和 response
                        cfg.EnableSensitiveData = hostEnvironment.IsDevelopment();
                    })
                .UseLogging(loggerFactory)
                .Build();

            return chatClient;
        });

        // 注册智能体实现
        services.AddScoped<IEvidenceGapAgent, EvidenceGapAgent>();
        services.AddScoped<IInspectionDecisionAgent, InspectionDecisionAgent>();

        // 注册业务函数与外部业务 API 客户端
        services.AddScoped<IInboundBusinessFunctions, InboundBusinessFunctions>();
        services.AddHttpClient<IBusinessApiClient, BusinessApiClient>((serviceProvider, client) =>
        {
            var inboundBaseUrl = configuration["Services:Inbound:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(inboundBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 配置基于 CAP 的事件总线
        services.AddCap(options =>
        {
            options.UseEntityFramework<AiDbContext>();
            options.UseRabbitMQ(rabbitOptions =>
            {
                rabbitOptions.ConnectionFactoryOptions = factory =>
                {
                    factory.Uri = new Uri(rabbitMqConnection);
                };
                rabbitOptions.ExchangeName = "wmsai.events";
            });
        });

        return services;
    }
}

public static class AiGatewayDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AiDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
