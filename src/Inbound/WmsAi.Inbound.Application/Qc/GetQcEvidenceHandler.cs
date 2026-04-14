using WmsAi.Inbound.Application.Abstractions;

namespace WmsAi.Inbound.Application.Qc;

public sealed class GetQcEvidenceHandler
{
    public async Task<List<EvidenceAssetDto>> Handle(
        Guid qcTaskId,
        string tenantId,
        string warehouseId,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现证据查询逻辑
        // 当前返回空列表，因为 QcTask 领域模型中还没有证据关联
        await Task.CompletedTask;
        return [];
    }
}

public sealed record EvidenceAssetDto(
    Guid AssetId,
    string Type,
    string Url,
    string? Metadata);
