using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Domain.MafSessions;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Persistence;
using WmsAi.AiGateway.Infrastructure.Repositories;
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
