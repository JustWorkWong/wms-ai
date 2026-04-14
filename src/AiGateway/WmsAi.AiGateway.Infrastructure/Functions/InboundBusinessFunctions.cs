using System.Text.Json;
using WmsAi.AiGateway.Application.Functions;
using WmsAi.AiGateway.Domain.Inspections;
using DotNetCore.CAP;

namespace WmsAi.AiGateway.Infrastructure.Functions;

public sealed class InboundBusinessFunctions : IInboundBusinessFunctions
{
    private readonly IBusinessApiClient _apiClient;
    private readonly IAiInspectionRunRepository _inspectionRepository;
    private readonly ICapPublisher _capPublisher;

    public InboundBusinessFunctions(
        IBusinessApiClient apiClient,
        IAiInspectionRunRepository inspectionRepository,
        ICapPublisher capPublisher)
    {
        _apiClient = apiClient;
        _inspectionRepository = inspectionRepository;
        _capPublisher = capPublisher;
    }

    public async Task<QcTaskDetails?> GetQcTaskDetailsAsync(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<QcTaskDetailsDto>(
            $"/api/inbound/qc/tasks/{qcTaskId}",
            tenantId,
            warehouseId,
            cancellationToken);

        if (response == null)
            return null;

        return new QcTaskDetails(
            response.QcTaskId,
            response.TaskNo,
            response.SkuCode,
            response.Quantity,
            response.Status,
            response.InboundNoticeId,
            response.ReceiptId);
    }

    public async Task<List<EvidenceAsset>> GetEvidenceAssetsAsync(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<List<EvidenceAssetDto>>(
            $"/api/inbound/qc/tasks/{qcTaskId}/evidence",
            tenantId,
            warehouseId,
            cancellationToken);

        if (response == null)
            return [];

        return response.Select(e => new EvidenceAsset(
            e.AssetId,
            e.Type,
            e.Url,
            e.Metadata)).ToList();
    }

    public async Task<QualityProfile?> GetQualityRulesAsync(
        string skuId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<QualityProfileDto>(
            $"/api/inbound/skus/{skuId}/quality-profile",
            tenantId,
            null,
            cancellationToken);

        if (response == null)
            return null;

        return new QualityProfile(
            response.SkuId,
            response.Rules.Select(r => new QualityRule(
                r.RuleType,
                r.Description,
                r.Threshold)).ToList());
    }

    public async Task<Guid> SubmitAiSuggestionAsync(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        string suggestionType,
        double confidence,
        string reasoning,
        CancellationToken cancellationToken = default)
    {
        var suggestion = new AiSuggestion(
            tenantId,
            warehouseId,
            qcTaskId,
            suggestionType,
            confidence,
            reasoning);

        await _inspectionRepository.AddSuggestionAsync(suggestion, cancellationToken);

        // Publish event
        await _capPublisher.PublishAsync(
            "wmsai.aigateway.aisuggestion.created.v1",
            new
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                TenantId = tenantId,
                WarehouseId = warehouseId,
                QcTaskId = qcTaskId,
                SuggestionId = suggestion.Id,
                SuggestedDecision = suggestionType,
                Reasoning = reasoning
            },
            cancellationToken: cancellationToken);

        return suggestion.Id;
    }
}

// Internal DTOs for API responses
internal sealed record QcTaskDetailsDto(
    Guid QcTaskId,
    string TaskNo,
    string SkuCode,
    decimal Quantity,
    string Status,
    Guid InboundNoticeId,
    Guid ReceiptId);

internal sealed record EvidenceAssetDto(
    Guid AssetId,
    string Type,
    string Url,
    string? Metadata);

internal sealed record QualityProfileDto(
    string SkuId,
    List<QualityRuleDto> Rules);

internal sealed record QualityRuleDto(
    string RuleType,
    string Description,
    string? Threshold);
