using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WmsAi.AiGateway.Infrastructure.Persistence;

/// <summary>
/// EF Core 设计时 DbContext 工厂（用于迁移）
/// </summary>
public sealed class AiDbContextFactory : IDesignTimeDbContextFactory<AiDbContext>
{
    public AiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AiDbContext>();

        // 使用本地开发连接字符串
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=wmsai_ai;Username=wmsai;Password=wmsai");

        return new AiDbContext(optionsBuilder.Options);
    }
}
