namespace WmsAi.Inbound.Application.Support;

public abstract class InboundException(string message) : Exception(message);

public sealed class InboundNotFoundException(string message) : InboundException(message);

public sealed class InboundConflictException(string message) : InboundException(message);

public sealed class InboundInvalidStateException(string message) : InboundException(message);

public sealed class InboundValidationException(string message) : InboundException(message);
