namespace WmsAi.AiGateway.Application.AgUi;

public sealed record ManualReviewRequest(
    string Decision,
    string Reasoning,
    string ReviewerId);
