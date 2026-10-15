using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SPFM.Api.Middleware;

/// <summary>
/// Turns an unhandled exception into an RFC 9457 ProblemDetails response.
///
/// The client gets a sentence it can show a person plus a traceId; the detail stays
/// in the server log. Requirements s22: consistent responses that do not expose
/// sensitive server information.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(
    ILogger<ProblemDetailsExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var traceId = context.TraceIdentifier;

        logger.LogError(exception,
            "Unhandled exception on {Method} {Path} (traceId {TraceId})",
            context.Request.Method, context.Request.Path, traceId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong",
                // Deliberately generic: exception text can leak connection strings,
                // file paths and internal structure.
                Detail = "The request could not be completed. Please try again.",
                Extensions = { ["traceId"] = traceId },
            },
        });
    }
}
