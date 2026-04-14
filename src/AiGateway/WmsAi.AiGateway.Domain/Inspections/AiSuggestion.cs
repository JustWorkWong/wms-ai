namespace WmsAi.AiGateway.Domain.Inspections;

public sealed class AiSuggestion
{
    private AiSuggestion()
    {
    }

    public AiSuggestion(
        string tenantId,
        string warehouseId,
        Guid qcTaskId,
        string suggestionType,
        double confidence,
        string reasoning)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(warehouseId);
        ArgumentOutOfRangeException.ThrowIfEqual(qcTaskId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasoning);
        ArgumentOutOfRangeException.ThrowIfNegative(confidence);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(confidence, 1.0);

        Id = Guid.NewGuid();
        TenantId = tenantId.Trim();
        WarehouseId = warehouseId.Trim();
        QcTaskId = qcTaskId;
        SuggestionType = suggestionType.Trim();
        Confidence = confidence;
        Reasoning = reasoning.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public string WarehouseId { get; private set; } = string.Empty;

    public Guid QcTaskId { get; private set; }

    public string SuggestionType { get; private set; } = string.Empty;

    public string Reasoning { get; private set; } = string.Empty;

    public double Confidence { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
