using Microsoft.AspNetCore.Http;
using WmsAi.Inbound.Application.Support;

namespace WmsAi.Inbound.Host;

public sealed record InboundHttpError(int StatusCode, string Title, string Detail);

public static class InboundHttpExceptionMapper
{
    public static InboundHttpError Map(Exception exception)
    {
        return exception switch
        {
            InboundNotFoundException notFound => new InboundHttpError(StatusCodes.Status404NotFound, "Not Found", notFound.Message),
            InboundConflictException conflict => new InboundHttpError(StatusCodes.Status409Conflict, "Conflict", conflict.Message),
            InboundInvalidStateException invalidState => new InboundHttpError(StatusCodes.Status409Conflict, "Invalid State", invalidState.Message),
            InboundValidationException validation => new InboundHttpError(StatusCodes.Status400BadRequest, "Bad Request", validation.Message),
            ArgumentException argument => new InboundHttpError(StatusCodes.Status400BadRequest, "Bad Request", argument.Message),
            _ => new InboundHttpError(StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred.")
        };
    }
}
