using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // 注册 AI 模型客户端（支持 OpenAI 兼容接口）
        services.AddHttpClient("AiModelClient", (serviceProvider, client) =>
        {
            var endpoint = configuration["AiProviders:Qwen:Endpoint"]
                ?? throw new InvalidOperationException("AiProviders:Qwen:Endpoint not configured");
            client.BaseAddress = new Uri(endpoint);
            client.Timeout = TimeSpan.FromSeconds(60);

            var apiKey = configuration["AiProviders:Qwen:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }
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
