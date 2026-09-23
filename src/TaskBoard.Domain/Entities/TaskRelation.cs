namespace TaskBoard.Domain.Entities;

/// <summary>
/// Symmetric relationship between two tasks. The pair is stored in a canonical order so that
/// (A, B) and (B, A) are the same relationship and can exist at most once.
/// </summary>
public sealed class TaskRelation
{
    private TaskRelation()
    {
    }

    private TaskRelation(Guid firstTaskId, Guid secondTaskId)
    {
        FirstTaskId = firstTaskId;
        SecondTaskId = secondTaskId;
    }

    public Guid FirstTaskId { get; private set; }

    public Guid SecondTaskId { get; private set; }

    public static TaskRelation Create(Guid taskId, Guid relatedTaskId)
    {
        if (taskId == relatedTaskId)
        {
            throw new ArgumentException("A task cannot be related to itself.", nameof(relatedTaskId));
        }

        return taskId.CompareTo(relatedTaskId) < 0
            ? new TaskRelation(taskId, relatedTaskId)
            : new TaskRelation(relatedTaskId, taskId);
    }

    public bool Involves(Guid taskId) => FirstTaskId == taskId || SecondTaskId == taskId;

    public Guid GetOtherTaskId(Guid taskId)
    {
        if (taskId == FirstTaskId)
        {
            return SecondTaskId;
        }

        if (taskId == SecondTaskId)
        {
            return FirstTaskId;
        }

        throw new ArgumentException("The task is not part of this relationship.", nameof(taskId));
    }
}
