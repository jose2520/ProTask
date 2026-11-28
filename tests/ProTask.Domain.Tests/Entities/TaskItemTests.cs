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
        var task = TaskItem.Create("Nueva tarea", null, TaskPriority.Media, NowUtc.AddDays(1));

        Assert.Equal(TaskItemStatus.Pendiente, task.Status);
        Assert.Equal(TaskPriority.Media, task.Priority);
    }

    [Fact]
    public void IsOverdue_EvaluatesStatusAndDueDateCorrectly()
    {
        var overduePending = TaskItem.Create("T1", null, TaskPriority.Media, NowUtc.AddHours(-1), status: TaskItemStatus.Pendiente);
        var overdueInProgress = TaskItem.Create("T2", null, TaskPriority.Media, NowUtc.AddDays(-1), status: TaskItemStatus.EnProgreso);
        var overdueCompleted = TaskItem.Create("T3", null, TaskPriority.Media, NowUtc.AddDays(-5), status: TaskItemStatus.Completada);
        var futurePending = TaskItem.Create("T4", null, TaskPriority.Media, NowUtc.AddHours(1), status: TaskItemStatus.Pendiente);

        Assert.True(overduePending.IsOverdue(NowUtc));
        Assert.True(overdueInProgress.IsOverdue(NowUtc));
        Assert.False(overdueCompleted.IsOverdue(NowUtc));
        Assert.False(futurePending.IsOverdue(NowUtc));
    }

    [Fact]
    public void IsOverdue_WhenDueDateEqualsReference_ReturnsFalse()
    {
        var task = TaskItem.Create("T1", null, TaskPriority.Media, NowUtc, status: TaskItemStatus.Pendiente);

        Assert.False(task.IsOverdue(NowUtc));
    }

    [Fact]
    public void ChangeStatus_WithValidStatus_UpdatesStatus()
    {
        var task = TaskItem.Create("T1", null, TaskPriority.Media, NowUtc.AddDays(1), status: TaskItemStatus.Pendiente);

        task.ChangeStatus(TaskItemStatus.EnProgreso);

        Assert.Equal(TaskItemStatus.EnProgreso, task.Status);
    }

    [Fact]
    public void ChangeStatus_WithUndefinedStatus_ThrowsBusinessRuleException()
    {
        var task = TaskItem.Create("T1", null, TaskPriority.Media, NowUtc.AddDays(1), status: TaskItemStatus.Pendiente);

        var exception = Assert.Throws<BusinessRuleException>(() => task.ChangeStatus((TaskItemStatus)999));

        Assert.Equal("invalid_task_status", exception.ErrorCode);
        Assert.Equal(TaskItemStatus.Pendiente, task.Status);
    }
}
