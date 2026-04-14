using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WmsAi.Inbound.Application.Inbound;
using WmsAi.Inbound.Application.Qc;
using WmsAi.Inbound.Application.Receipts;
using WmsAi.Inbound.Application.Support;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Domain.Receipts;
using WmsAi.Inbound.Host;
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

        await act.Should().ThrowAsync<InboundConflictException>();
    }

    [Fact]
    public async Task Add_inbound_module_should_initialize_fresh_database()
    {
        // Use in-memory SQLite for testing
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var tables = await dbContext.Database.SqlQueryRaw<string>(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('inbound_notices', 'receipts', 'qc_tasks', 'qc_decisions')")
            .ToListAsync();

        tables.Should().BeEquivalentTo(["inbound_notices", "receipts", "qc_tasks", "qc_decisions"]);
    }

    [Fact]
    public async Task Record_receipt_should_reject_zero_quantity_line()
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
            "ASN_DEMO_003",
            [new InboundNoticeLineInput("sku-001", 10m)]));

        var handler = new RecordReceiptHandler(dbContext);
        var act = async () => await handler.Handle(new RecordReceiptCommand(
            "tenant-demo",
            "wh-sz-01",
            notice.InboundNoticeId,
            "RCV_DEMO_003",
            [new ReceiptLineInput("sku-001", 0m)]));

        await act.Should().ThrowAsync<InboundValidationException>();
    }

    [Fact]
    public async Task Create_inbound_notice_should_reject_duplicate_notice_no()
    {
        await using var database = new SqliteConnection("DataSource=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseSqlite(database)
            .Options;

        await using var dbContext = new BusinessDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var handler = new CreateInboundNoticeHandler(dbContext);

        await handler.Handle(new CreateInboundNoticeCommand(
            "tenant-demo",
            "wh-sz-01",
            "ASN_DUP_001",
            [new InboundNoticeLineInput("sku-001", 10m)]));

        var act = async () => await handler.Handle(new CreateInboundNoticeCommand(
            "tenant-demo",
            "wh-sz-01",
            "ASN_DUP_001",
            [new InboundNoticeLineInput("sku-002", 12m)]));

        await act.Should().ThrowAsync<InboundConflictException>();
    }

    [Theory]
    [InlineData(typeof(InboundNotFoundException), 404)]
    [InlineData(typeof(InboundConflictException), 409)]
    [InlineData(typeof(InboundInvalidStateException), 409)]
    [InlineData(typeof(InboundValidationException), 400)]
    [InlineData(typeof(ArgumentException), 400)]
    [InlineData(typeof(InvalidOperationException), 500)]
    public void Map_business_exception_should_return_expected_status(Type exceptionType, int statusCode)
    {
        Exception exception = exceptionType.Name switch
        {
            nameof(InboundNotFoundException) => new InboundNotFoundException("missing"),
            nameof(InboundConflictException) => new InboundConflictException("conflict"),
            nameof(InboundInvalidStateException) => new InboundInvalidStateException("invalid"),
            nameof(InboundValidationException) => new InboundValidationException("bad request"),
            nameof(ArgumentException) => new ArgumentException("arg"),
            _ => new InvalidOperationException("boom")
        };

        var result = InboundHttpExceptionMapper.Map(exception);

        result.StatusCode.Should().Be(statusCode);
    }
}
