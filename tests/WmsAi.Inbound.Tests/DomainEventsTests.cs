using FluentAssertions;
using WmsAi.Inbound.Domain.Inbound;
using WmsAi.Inbound.Domain.Qc;
using WmsAi.Inbound.Domain.Receipts;
using Xunit;

namespace WmsAi.Inbound.Tests;

public class DomainEventsTests
{
    [Fact]
    public void InboundNotice_creation_should_raise_domain_event()
    {
        var lines = new[] { new InboundNoticeLineInput("SKU001", 100) };
        var notice = new InboundNotice("tenant1", "wh1", "IB001", lines);

        notice.DomainEvents.Should().ContainSingle();
        var domainEvent = notice.DomainEvents.First();
        domainEvent.Should().BeOfType<InboundNoticeCreatedEvent>();

        var noticeCreatedEvent = (InboundNoticeCreatedEvent)domainEvent;
        noticeCreatedEvent.InboundNoticeId.Should().Be(notice.Id);
        noticeCreatedEvent.TenantId.Should().Be("tenant1");
        noticeCreatedEvent.WarehouseId.Should().Be("wh1");
        noticeCreatedEvent.NoticeNo.Should().Be("IB001");
    }

    [Fact]
    public void Receipt_creation_should_raise_domain_event()
    {
        var noticeId = Guid.NewGuid();
        var lines = new[] { new ReceiptLineInput("SKU001", 100) };
        var receipt = new Receipt("tenant1", "wh1", noticeId, "RCP001", lines);

        receipt.DomainEvents.Should().ContainSingle();
        var domainEvent = receipt.DomainEvents.First();
        domainEvent.Should().BeOfType<ReceiptRecordedEvent>();

        var receiptRecordedEvent = (ReceiptRecordedEvent)domainEvent;
        receiptRecordedEvent.ReceiptId.Should().Be(receipt.Id);
        receiptRecordedEvent.InboundNoticeId.Should().Be(noticeId);
        receiptRecordedEvent.ReceiptNo.Should().Be("RCP001");
    }

    [Fact]
    public void QcTask_creation_should_raise_domain_event()
    {
        var noticeId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var qcTask = new QcTask("tenant1", "wh1", noticeId, receiptId, "SKU001", "QC001");

        qcTask.DomainEvents.Should().ContainSingle();
        var domainEvent = qcTask.DomainEvents.First();
        domainEvent.Should().BeOfType<QcTaskCreatedEvent>();

        var qcTaskCreatedEvent = (QcTaskCreatedEvent)domainEvent;
        qcTaskCreatedEvent.QcTaskId.Should().Be(qcTask.Id);
        qcTaskCreatedEvent.InboundNoticeId.Should().Be(noticeId);
        qcTaskCreatedEvent.ReceiptId.Should().Be(receiptId);
        qcTaskCreatedEvent.TaskNo.Should().Be("QC001");
    }

    [Fact]
    public void QcTask_finalize_should_raise_domain_event()
    {
        var noticeId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var qcTask = new QcTask("tenant1", "wh1", noticeId, receiptId, "SKU001", "QC001");
        qcTask.ClearDomainEvents();

        var decisionId = Guid.NewGuid();
        qcTask.Finalize(decisionId, "accepted");

        qcTask.DomainEvents.Should().ContainSingle();
        var domainEvent = qcTask.DomainEvents.First();
        domainEvent.Should().BeOfType<QcDecisionFinalizedEvent>();

        var qcDecisionFinalizedEvent = (QcDecisionFinalizedEvent)domainEvent;
        qcDecisionFinalizedEvent.QcTaskId.Should().Be(qcTask.Id);
        qcDecisionFinalizedEvent.QcDecisionId.Should().Be(decisionId);
        qcDecisionFinalizedEvent.DecisionStatus.Should().Be("accepted");
    }
}
