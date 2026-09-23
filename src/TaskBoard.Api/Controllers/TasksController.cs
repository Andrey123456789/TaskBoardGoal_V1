using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Services;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Api.Controllers;

[Route("api/tasks")]
public sealed class TasksController(ITaskService tasks) : ApiControllerBase
{
    /// <summary>Lists tasks, optionally filtered by project, assignee and status (combinable).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskSummaryDto>>> List(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assigneeId,
        [FromQuery] TaskItemStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await tasks.ListAsync(
            new TaskFilter(projectId, assigneeId, status),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await tasks.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>Creates a task in the Created status, optionally related to existing tasks.</summary>
    [HttpPost]
    [ProducesResponseType<TaskDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskDetailsDto>> Create(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tasks.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error);
    }

    /// <summary>Edits title, description and assignee. Not allowed for Completed or Closed tasks.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Update(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tasks.UpdateAsync(id, request, cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>Performs the next workflow transition: Created → InProgress → Completed → Closed.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> ChangeStatus(
        Guid id,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tasks.ChangeStatusAsync(id, request, cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>Replaces the complete set of related tasks. Allowed in every task status.</summary>
    [HttpPut("{id:guid}/related-tasks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> ReplaceRelatedTasks(
        Guid id,
        ReplaceRelatedTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tasks.ReplaceRelatedTasksAsync(id, request, cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>Deletes a Created or InProgress task together with its relationships.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await tasks.DeleteAsync(id, cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
