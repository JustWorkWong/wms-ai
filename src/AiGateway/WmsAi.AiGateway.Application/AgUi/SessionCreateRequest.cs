namespace WmsAi.AiGateway.Application.AgUi;

public sealed record SessionCreateRequest(
    string TenantId,
    string WarehouseId,
    string UserId,
    string BusinessObjectType,
    string BusinessObjectId,
    string? SceneCode = null);
