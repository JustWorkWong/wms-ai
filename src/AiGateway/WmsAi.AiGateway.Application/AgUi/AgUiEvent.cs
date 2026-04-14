namespace WmsAi.AiGateway.Application.AgUi;

public abstract record AgUiEvent(
    string Type,
    DateTimeOffset Timestamp);
