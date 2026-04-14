using FluentAssertions;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Domain.Receipts;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Infrastructure.Persistence;
using Xunit;

namespace WmsAi.Inbound.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task InboundNoticeRepository_should_add_and_retrieve_notice()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new InboundNoticeRepository(dbContext);
        var lines = new[] { new InboundNoticeLineInput("SKU001", 100) };
        var notice = new InboundNotice("tenant1", "wh1", "IB001", lines);

        await repository.AddAsync(notice);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(notice.Id);
        retrieved.Should().NotBeNull();
        retrieved!.NoticeNo.Should().Be("IB001");
        retrieved.Lines.Should().HaveCount(1);

        var retrievedByNoticeNo = await repository.GetByNoticeNoAsync("tenant1", "wh1", "IB001");
        retrievedByNoticeNo.Should().NotBeNull();
        retrievedByNoticeNo!.Id.Should().Be(notice.Id);

        var exists = await repository.ExistsByNoticeNoAsync("tenant1", "wh1", "IB001");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiptRepository_should_add_and_retrieve_receipt()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var noticeLines = new[] { new InboundNoticeLineInput("SKU001", 100) };
        var notice = new InboundNotice("tenant1", "wh1", "IB001", noticeLines);
        dbContext.InboundNotices.Add(notice);
        await dbContext.SaveChangesAsync();

        var repository = new ReceiptRepository(dbContext);
        var receiptLines = new[] { new ReceiptLineInput("SKU001", 100) };
        var receipt = new Receipt("tenant1", "wh1", notice.Id, "RCP001", receiptLines);

        await repository.AddAsync(receipt);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(receipt.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ReceiptNo.Should().Be("RCP001");
        retrieved.Lines.Should().HaveCount(1);

        var retrievedByReceiptNo = await repository.GetByReceiptNoAsync("tenant1", "wh1", "RCP001");
        retrievedByReceiptNo.Should().NotBeNull();
        retrievedByReceiptNo!.Id.Should().Be(receipt.Id);

        var exists = await repository.ExistsByReceiptNoAsync("tenant1", "wh1", "RCP001");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task QcTaskRepository_should_add_and_retrieve_task()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var noticeLines = new[] { new InboundNoticeLineInput("SKU001", 100) };
        var notice = new InboundNotice("tenant1", "wh1", "IB001", noticeLines);
        dbContext.InboundNotices.Add(notice);

        var receiptLines = new[] { new ReceiptLineInput("SKU001", 100) };
        var receipt = new Receipt("tenant1", "wh1", notice.Id, "RCP001", receiptLines);
        dbContext.Receipts.Add(receipt);
        await dbContext.SaveChangesAsync();

        var repository = new QcTaskRepository(dbContext);
        var task = new QcTask("tenant1", "wh1", notice.Id, receipt.Id, "SKU001", "QC001");

        await repository.AddAsync(task);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(task.Id);
        retrieved.Should().NotBeNull();
        retrieved!.TaskNo.Should().Be("QC001");

        var retrievedByTaskNo = await repository.GetByTaskNoAsync("tenant1", "wh1", "QC001");
        retrievedByTaskNo.Should().NotBeNull();
        retrievedByTaskNo!.Id.Should().Be(task.Id);

        var retrievedByReceiptId = await repository.GetByReceiptIdAsync(receipt.Id);
        retrievedByReceiptId.Should().ContainSingle();
        retrievedByReceiptId[0].Id.Should().Be(task.Id);

        var exists = await repository.ExistsByTaskNoAsync("tenant1", "wh1", "QC001");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task QcDecisionRepository_should_add_and_retrieve_decision()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var noticeLines = new[] { new InboundNoticeLineInput("SKU001", 100) };
        var notice = new InboundNotice("tenant1", "wh1", "IB001", noticeLines);
        dbContext.InboundNotices.Add(notice);

        var receiptLines = new[] { new ReceiptLineInput("SKU001", 100) };
        var receipt = new Receipt("tenant1", "wh1", notice.Id, "RCP001", receiptLines);
        dbContext.Receipts.Add(receipt);

        var task = new QcTask("tenant1", "wh1", notice.Id, receipt.Id, "SKU001", "QC001");
        dbContext.QcTasks.Add(task);
        await dbContext.SaveChangesAsync();

        var repository = new QcDecisionRepository(dbContext);
        var decision = new QcDecision("tenant1", "wh1", task.Id, "accepted", "manual", "Good quality");

        await repository.AddAsync(decision);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(decision.Id);
        retrieved.Should().NotBeNull();
        retrieved!.QcTaskId.Should().Be(task.Id);

        var retrievedByQcTaskId = await repository.GetByQcTaskIdAsync(task.Id);
        retrievedByQcTaskId.Should().NotBeNull();
        retrievedByQcTaskId!.Id.Should().Be(decision.Id);
    }
}
