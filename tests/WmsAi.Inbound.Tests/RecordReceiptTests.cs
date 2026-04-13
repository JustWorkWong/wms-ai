using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.Inbound.Application.Inbound;
using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Domain.Receipts;
using WmsAi.Inbound.Infrastructure.Persistence;
using Xunit;

namespace WmsAi.Inbound.Tests;

public class RecordReceiptTests
{
    [Fact]
    public async Task Record_receipt_should_create_qc_tasks()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var createInboundNoticeHandler = new CreateInboundNoticeHandler(dbContext);
        var notice = await createInboundNoticeHandler.Handle(new CreateInboundNoticeCommand(
            "tenant-demo",
            "wh-sz-01",
            "ASN_DEMO_001",
            [new InboundNoticeLineInput("sku-001", 100m)]));

        var handler = new RecordReceiptHandler(dbContext);

        var result = await handler.Handle(new RecordReceiptCommand(
            "tenant-demo",
            "wh-sz-01",
            notice.InboundNoticeId,
            "RCV_DEMO_001",
            [new ReceiptLineInput("sku-001", 100m)]));

        result.QcTaskCount.Should().Be(1);
        dbContext.QcTasks.Should().ContainSingle();
    }

    [Fact]
    public async Task Finalize_qc_decision_should_reject_duplicate_formal_decision()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var createInboundNoticeHandler = new CreateInboundNoticeHandler(dbContext);
        var notice = await createInboundNoticeHandler.Handle(new CreateInboundNoticeCommand(
            "tenant-demo",
            "wh-sz-01",
            "ASN_DEMO_002",
            [new InboundNoticeLineInput("sku-001", 20m)]));

        var recordReceiptHandler = new RecordReceiptHandler(dbContext);
        await recordReceiptHandler.Handle(new RecordReceiptCommand(
            "tenant-demo",
            "wh-sz-01",
            notice.InboundNoticeId,
            "RCV_DEMO_002",
            [new ReceiptLineInput("sku-001", 20m)]));

        var qcTask = await dbContext.QcTasks.SingleAsync();
        var finalizeHandler = new FinalizeQcDecisionHandler(dbContext);

        await finalizeHandler.Handle(new FinalizeQcDecisionCommand(
            "tenant-demo",
            "wh-sz-01",
            qcTask.Id,
            "accepted",
            "system",
            "Auto pass"));

        var act = async () => await finalizeHandler.Handle(new FinalizeQcDecisionCommand(
            "tenant-demo",
            "wh-sz-01",
            qcTask.Id,
            "rejected",
            "reviewer",
            "Manual override"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Add_inbound_module_should_initialize_fresh_database()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:BusinessDb"] = $"Data Source={databasePath}"
                })
                .Build();

            services.AddInboundModule(configuration);

            await using var provider = services.BuildServiceProvider();
            await BusinessDatabaseInitializer.InitializeAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();

            var tables = await dbContext.Database.SqlQueryRaw<string>(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('inbound_notices', 'receipts', 'qc_tasks', 'qc_decisions')")
                .ToListAsync();

            tables.Should().BeEquivalentTo(["inbound_notices", "receipts", "qc_tasks", "qc_decisions"]);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
