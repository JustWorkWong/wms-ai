namespace WmsAi.AiGateway.Application.AgUi;

public sealed record SessionResumeRequest(
    Guid? CheckpointId = null);
