using TaskBoard.Application.DTOs.Projects;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Validation;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Tests.Validation;

[TestFixture]
public sealed class TaskRequestValidatorTests
{
    [Test]
    public void CreateTask_WithMinimalValidRequest_Passes()
    {
        var result = new CreateTaskRequestValidator().Validate(
            new CreateTaskRequest("Title", null, Guid.NewGuid(), null, null));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void CreateTask_WithoutTitleOrProject_FailsOnBoth()
    {
        var result = new CreateTaskRequestValidator().Validate(
            new CreateTaskRequest(" ", null, Guid.Empty, null, []));

        Assert.That(
            result.Errors.Select(e => e.PropertyName),
            Is.EquivalentTo(new[] { "Title", "ProjectId" }));
    }

    [Test]
    public void CreateTask_WithTooLongTitleAndDescription_Fails()
    {
        var result = new CreateTaskRequestValidator().Validate(
            new CreateTaskRequest(
                new string('t', TaskItem.TitleMaxLength + 1),
                new string('d', TaskItem.DescriptionMaxLength + 1),
                Guid.NewGuid(),
                null,
                null));

        Assert.That(
            result.Errors.Select(e => e.PropertyName),
            Is.EquivalentTo(new[] { "Title", "Description" }));
    }

    [Test]
    public void UpdateTask_WithoutTitle_Fails()
    {
        var result = new UpdateTaskRequestValidator().Validate(new UpdateTaskRequest("", null, null));

        Assert.That(result.Errors.Select(e => e.PropertyName), Is.EquivalentTo(new[] { "Title" }));
    }

    [Test]
    public void ChangeStatus_WithoutStatus_Fails()
    {
        var result = new ChangeTaskStatusRequestValidator().Validate(new ChangeTaskStatusRequest(null));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ChangeStatus_WithUndefinedStatus_Fails()
    {
        var result = new ChangeTaskStatusRequestValidator().Validate(
            new ChangeTaskStatusRequest((TaskItemStatus)42));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ReplaceRelatedTasks_WithEmptyList_Passes()
    {
        var result = new ReplaceRelatedTasksRequestValidator().Validate(new ReplaceRelatedTasksRequest([]));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ReplaceRelatedTasks_WithoutList_Fails()
    {
        var result = new ReplaceRelatedTasksRequestValidator().Validate(new ReplaceRelatedTasksRequest(null!));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void TaskFilter_WithUndefinedStatus_Fails()
    {
        var result = new TaskFilterValidator().Validate(new TaskFilter(null, null, (TaskItemStatus)42));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void TaskFilter_WithoutCriteria_Passes()
    {
        var result = new TaskFilterValidator().Validate(new TaskFilter(null, null, null));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void SaveProject_WithoutName_Fails()
    {
        var result = new SaveProjectRequestValidator().Validate(new SaveProjectRequest("", "Description"));

        Assert.That(result.Errors.Select(e => e.PropertyName), Is.EquivalentTo(new[] { "Name" }));
    }

    [Test]
    public void SaveProject_WithoutDescription_Passes()
    {
        var result = new SaveProjectRequestValidator().Validate(new SaveProjectRequest("Project", null));

        Assert.That(result.IsValid, Is.True);
    }
}
