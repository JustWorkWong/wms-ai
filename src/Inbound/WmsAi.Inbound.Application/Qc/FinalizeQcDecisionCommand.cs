using Microsoft.EntityFrameworkCore;
using WmsAi.Inbound.Application.Abstractions;
using WmsAi.Inbound.Application.Support;
using WmsAi.Inbound.Domain.Qc;

namespace WmsAi.Inbound.Application.Qc;

public sealed record FinalizeQcDecisionCommand(
    string TenantId,
    string WarehouseId,
    Guid QcTaskId,
    string DecisionStatus,
    string DecisionSource,
    string ReasonSummary);

public sealed record FinalizeQcDecisionResult(Guid QcDecisionId, string TaskStatus);

public sealed record QcTaskSummary(
    Guid QcTaskId,
    string TaskNo,
    string SkuCode,
    string Status,
    Guid? QcDecisionId);

public sealed class FinalizeQcDecisionHandler(IBusinessDbContext businessDbContext)
{
    public async Task<FinalizeQcDecisionResult> Handle(
        FinalizeQcDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        var qcTask = await businessDbContext.QcTasks
            .SingleOrDefaultAsync(
                entity => entity.Id == command.QcTaskId
                    && entity.TenantId == command.TenantId
                    && entity.WarehouseId == command.WarehouseId,
                cancellationToken);

        if (qcTask is null)
        {
            throw new InboundNotFoundException("Qc task was not found.");
        }

        QcDecision decision;
        try
        {
            decision = new QcDecision(
                command.TenantId,
                command.WarehouseId,
                qcTask.Id,
                command.DecisionStatus,
                command.DecisionSource,
                command.ReasonSummary);
        }
        catch (ArgumentException exception)
        {
            throw new InboundValidationException(exception.Message);
        }

        try
        {
            qcTask.Finalize(decision.Id, command.DecisionStatus);
        }
        catch (InvalidOperationException exception)
        {
            throw new InboundConflictException(exception.Message);
        }
        businessDbContext.QcDecisions.Add(decision);

        var receipt = await businessDbContext.Receipts
            .SingleAsync(entity => entity.Id == qcTask.ReceiptId, cancellationToken);

        var hasOpenTasks = await businessDbContext.QcTasks.AnyAsync(
            entity => entity.ReceiptId == qcTask.ReceiptId
                && entity.Id != qcTask.Id
                && entity.QcDecisionId == null,
            cancellationToken);

        if (!hasOpenTasks)
        {
            receipt.MarkQcCompleted();
        }

        try
        {
            await businessDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (InboundPersistenceExceptionTranslator.Translate(exception, "Qc decision already exists.") is InboundConflictException translated)
        {
            throw translated;
        }

        return new FinalizeQcDecisionResult(decision.Id, qcTask.Status.ToString());
    }
}

public sealed class GetQcTasksHandler(IBusinessDbContext businessDbContext)
{
    public Task<List<QcTaskSummary>> Handle(
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default)
    {
        return businessDbContext.QcTasks
            .Where(entity => entity.TenantId == tenantId && entity.WarehouseId == warehouseId)
            .OrderBy(entity => entity.TaskNo)
            .Select(entity => new QcTaskSummary(
                entity.Id,
                entity.TaskNo,
                entity.SkuCode,
                entity.Status.ToString(),
                entity.QcDecisionId))
            .ToListAsync(cancellationToken);
    }
}
