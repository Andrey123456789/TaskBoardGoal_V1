using Microsoft.AspNetCore.Diagnostics;
using TaskBoard.Application.Abstractions.Persistence;

namespace TaskBoard.Api.ErrorHandling;

/// <summary>
/// Maps concurrent-modification conflicts detected while saving (for example, a duplicate email
/// inserted by a parallel request) to 409 Conflict.
/// </summary>
internal sealed class PersistenceConflictExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<PersistenceConflictExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not PersistenceConflictException)
        {
            return false;
        }

        logger.LogWarning(
            exception,
            "Concurrent data conflict while processing {RequestMethod} {RequestPath}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict with the current state",
                Detail = "The data was changed by another request. Reload and try again.",
                Extensions = { ["code"] = "persistence.conflict" },
            },
        });
    }
}
