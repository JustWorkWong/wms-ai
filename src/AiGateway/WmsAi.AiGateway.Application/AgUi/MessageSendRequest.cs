namespace WmsAi.AiGateway.Application.AgUi;

public sealed record MessageSendRequest(
    string Content,
    List<MessageAttachment>? Attachments = null);

public sealed record MessageAttachment(
    string Type,
    string Url,
    string? Metadata = null);
