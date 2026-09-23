using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Results;

namespace TaskBoard.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Translates an expected application error into a ProblemDetails response.
    /// </summary>
    protected ActionResult Problem(Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            foreach (var (field, messages) in error.ValidationErrors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(field, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        var (statusCode, title) = error.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict with the current state"),
            ErrorType.Unprocessable => (StatusCodes.Status422UnprocessableEntity, "Unprocessable request"),
            _ => throw new InvalidOperationException($"Unsupported error type '{error.Type}'."),
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode: statusCode,
            title: title,
            detail: error.Message);

        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
