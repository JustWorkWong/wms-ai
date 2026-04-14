using WmsAi.Inbound.Application.Abstractions;

namespace WmsAi.Inbound.Application.Qc;

public sealed class GetSkuQualityProfileHandler
{
    public async Task<QualityProfileDto?> Handle(
        string skuCode,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现 SKU 质量规则查询
        // 当前返回默认规则
        await Task.CompletedTask;

        return new QualityProfileDto(
            skuCode,
            [
                new QualityRuleDto("Visual", "外观检查", null),
                new QualityRuleDto("Measurement", "尺寸测量", "95-105")
            ]);
    }
}

public sealed record QualityProfileDto(
    string SkuId,
    List<QualityRuleDto> Rules);

public sealed record QualityRuleDto(
    string RuleType,
    string Description,
    string? Threshold);
