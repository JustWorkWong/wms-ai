using WmsAi.SharedKernel.Domain;

namespace WmsAi.AiGateway.Domain.ModelConfig;

public sealed class AiRoutingPolicy : TenantScopedAggregateRoot
{
    private AiRoutingPolicy()
    {
    }

    public AiRoutingPolicy(
        string tenantId,
        string policyName,
        string sceneCode,
        string? warehouseId,
        string routingRulesJson,
        int priority,
        bool isActive)
        : base(tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingRulesJson);

        PolicyName = policyName.Trim();
        SceneCode = sceneCode.Trim();
        WarehouseId = warehouseId?.Trim();
        RoutingRulesJson = routingRulesJson.Trim();
        Priority = priority;
        IsActive = isActive;
    }

    public string PolicyName { get; private set; } = string.Empty;

    public string SceneCode { get; private set; } = string.Empty;

    public string? WarehouseId { get; private set; }

    public string RoutingRulesJson { get; private set; } = string.Empty;

    public int Priority { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateRules(string routingRulesJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingRulesJson);
        RoutingRulesJson = routingRulesJson.Trim();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
