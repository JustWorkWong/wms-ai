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
        // 读取连接串
        var userDbConnection = configuration.GetConnectionString("UserDb")
            ?? "Host=localhost;Database=UserDb;Username=postgres;Password=postgres";
        var businessDbConnection = configuration.GetConnectionString("BusinessDb")
            ?? "Host=localhost;Database=BusinessDb;Username=postgres;Password=postgres";
        var aiDbConnection = configuration.GetConnectionString("AiDb")
            ?? "Host=localhost;Database=AiDb;Username=postgres;Password=postgres";
        var hangfireConnection = configuration.GetConnectionString("HangfireDb")
            ?? userDbConnection; // 开发期直接复用 UserDb 存储 Hangfire 元数据

        // 注册三个业务库上下文
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

        // 注册 Hangfire
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

        // 注册启动阶段需要的后台服务
        services.AddHostedService<StartupBootstrapper>();
        services.AddHostedService<JobScheduler>();
        services.AddScoped<SeedDataImporter>();

        // 注册作业处理器
        services.AddScoped<Jobs.ScanPendingAiRunsJob>();
        services.AddScoped<Jobs.BuildDailyQcMetricsJob>();
        services.AddScoped<Jobs.CleanupExpiredSessionsJob>();

        return services;
    }

    public static IConfigurationBuilder AddNacosConfiguration(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        // Nacos 配置在本地开发环境默认关闭
        // 如需启用，请在 appsettings.json 中设置 Nacos:Enabled = true
        var nacosEnabled = configuration.GetValue<bool>("Nacos:Enabled", false);
        if (!nacosEnabled)
        {
            return builder;
        }

        // TODO: 需要时再接入 Nacos SDK

        return builder;
    }
}
