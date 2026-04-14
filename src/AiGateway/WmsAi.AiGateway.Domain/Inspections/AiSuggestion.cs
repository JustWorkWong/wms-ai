namespace WmsAi.AiGateway.Domain.Inspections;

public sealed class AiSuggestion
{
    private AiSuggestion()
    {
    }

    public AiSuggestion(
        Guid inspectionRunId,
        SuggestionType suggestionType,
        string reasoning,
        decimal confidenceScore,
        string? structuredDataJson)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(inspectionRunId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasoning);
        ArgumentOutOfRangeException.ThrowIfNegative(confidenceScore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(confidenceScore, 1.0m);

        Id = Guid.NewGuid();
        InspectionRunId = inspectionRunId;
        SuggestionType = suggestionType;
        Reasoning = reasoning.Trim();
        ConfidenceScore = confidenceScore;
        StructuredDataJson = structuredDataJson?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid InspectionRunId { get; private set; }

    public SuggestionType SuggestionType { get; private set; }

    public string Reasoning { get; private set; } = string.Empty;

    public decimal ConfidenceScore { get; private set; }

    public string? StructuredDataJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
