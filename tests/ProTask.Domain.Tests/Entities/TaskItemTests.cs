using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using Xunit;

namespace ProTask.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public sealed class TaskItemTests
{
    private static readonly DateTime NowUtc = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewTask_HasPendienteStatusAndMediaPriorityByDefault()
    {
        var task = new TaskItem();

        Assert.Equal(TaskItemStatus.Pendiente, task.Status);
        Assert.Equal(TaskPriority.Media, task.Priority);
    }

    [Fact]
    public void IsOverdue_EvaluatesStatusAndDueDateCorrectly()
    {
        var overduePending = new TaskItem { Status = TaskItemStatus.Pendiente, DueDate = NowUtc.AddHours(-1) };
        var overdueInProgress = new TaskItem { Status = TaskItemStatus.EnProgreso, DueDate = NowUtc.AddDays(-1) };
        var overdueCompleted = new TaskItem { Status = TaskItemStatus.Completada, DueDate = NowUtc.AddDays(-5) };
        var futurePending = new TaskItem { Status = TaskItemStatus.Pendiente, DueDate = NowUtc.AddHours(1) };

        Assert.True(overduePending.IsOverdue(NowUtc));
        Assert.True(overdueInProgress.IsOverdue(NowUtc));
        Assert.False(overdueCompleted.IsOverdue(NowUtc));
        Assert.False(futurePending.IsOverdue(NowUtc));
    }

    [Fact]
    public void IsOverdue_WhenDueDateEqualsReference_ReturnsFalse()
    {
        var task = new TaskItem { Status = TaskItemStatus.Pendiente, DueDate = NowUtc };

        Assert.False(task.IsOverdue(NowUtc));
    }

    [Fact]
    public void ChangeStatus_WithValidStatus_UpdatesStatus()
    {
        var task = new TaskItem { Status = TaskItemStatus.Pendiente };

        task.ChangeStatus(TaskItemStatus.EnProgreso);

        Assert.Equal(TaskItemStatus.EnProgreso, task.Status);
    }

    [Fact]
    public void ChangeStatus_WithUndefinedStatus_ThrowsBusinessRuleException()
    {
        var task = new TaskItem { Status = TaskItemStatus.Pendiente };

        var exception = Assert.Throws<BusinessRuleException>(() => task.ChangeStatus((TaskItemStatus)999));

        Assert.Equal("invalid_task_status", exception.ErrorCode);
        Assert.Equal(TaskItemStatus.Pendiente, task.Status);
    }
}
