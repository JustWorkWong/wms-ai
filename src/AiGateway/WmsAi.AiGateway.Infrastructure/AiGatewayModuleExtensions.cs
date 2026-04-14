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

        // Register repositories
        services.AddScoped<IMafSessionRepository, MafSessionRepository>();
        services.AddScoped<IMafWorkflowRunRepository, MafWorkflowRunRepository>();
        services.AddScoped<IAiInspectionRunRepository, AiInspectionRunRepository>();
        services.AddScoped<IAiModelProfileRepository, AiModelProfileRepository>();
        services.AddScoped<IAiModelProviderRepository, AiModelProviderRepository>();

        // Register services
        services.AddScoped<IMafPersistenceService, MafPersistenceService>();
        services.AddScoped<IModelRoutingService, ModelRoutingService>();
        services.AddScoped<IAgUiEventStreamService, AgUiEventStreamService>();
        services.AddScoped<IAgUiEventTransformer, AgUiEventTransformer>();

        // Register agents
        services.AddScoped<IEvidenceGapAgent, EvidenceGapAgent>();
        services.AddScoped<IInspectionDecisionAgent, InspectionDecisionAgent>();

        // Register business functions
        services.AddScoped<IInboundBusinessFunctions, InboundBusinessFunctions>();
        services.AddHttpClient<IBusinessApiClient, BusinessApiClient>((serviceProvider, client) =>
        {
            var inboundBaseUrl = configuration["Services:Inbound:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(inboundBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configure CAP for event bus
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
