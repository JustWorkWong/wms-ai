using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Infrastructure.Persistence;
using WmsAi.Inbound.Infrastructure.Persistence;
using WmsAi.Operations.Host.Services;
using WmsAi.Platform.Infrastructure.Persistence;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.Operations.Host;

public static class OperationsModuleExtensions
{
    public static IServiceCollection AddOperationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get connection strings
        var userDbConnection = configuration.GetConnectionString("UserDb")
            ?? "Host=localhost;Database=UserDb;Username=postgres;Password=postgres";
        var businessDbConnection = configuration.GetConnectionString("BusinessDb")
            ?? "Host=localhost;Database=BusinessDb;Username=postgres;Password=postgres";
        var aiDbConnection = configuration.GetConnectionString("AiDb")
            ?? "Host=localhost;Database=AiDb;Username=postgres;Password=postgres";
        var hangfireConnection = configuration.GetConnectionString("HangfireDb")
            ?? userDbConnection; // Use UserDb for Hangfire storage

        // Add DbContexts (read-only access to all three databases)
        services.AddSingleton<VersionedEntitySaveChangesInterceptor>();

        services.AddDbContext<UserDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(userDbConnection);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
        });

        services.AddDbContext<BusinessDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(businessDbConnection);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
        });

        services.AddDbContext<AiDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(aiDbConnection);
            options.AddInterceptors(serviceProvider.GetRequiredService<VersionedEntitySaveChangesInterceptor>());
        });

        // Add Hangfire
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(hangfireConnection);
            });
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.ServerName = "WmsAi.Operations";
        });

        // Add services
        services.AddHostedService<StartupBootstrapper>();
        services.AddHostedService<JobScheduler>();
        services.AddScoped<SeedDataImporter>();

        // Add jobs
        services.AddScoped<Jobs.ScanPendingAiRunsJob>();
        services.AddScoped<Jobs.BuildDailyQcMetricsJob>();
        services.AddScoped<Jobs.CleanupExpiredSessionsJob>();

        return services;
    }

    public static IConfigurationBuilder AddNacosConfiguration(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        // Nacos configuration is optional and disabled by default
        // To enable: set Nacos:Enabled = true in appsettings.json
        var nacosEnabled = configuration.GetValue<bool>("Nacos:Enabled", false);
        if (!nacosEnabled)
        {
            return builder;
        }

        // TODO: Implement Nacos configuration when needed
        // Requires nacos-sdk-csharp.AspNetCore package

        return builder;
    }
}
