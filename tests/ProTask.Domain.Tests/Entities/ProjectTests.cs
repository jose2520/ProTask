using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using Xunit;

namespace ProTask.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public sealed class ProjectTests
{
    [Fact]
    public void CalculateProgressPercentage_WithoutTasks_ReturnsZero()
    {
        var project = new Project { Id = 1, Name = "Sin tareas" };

        Assert.Equal(0d, project.CalculateProgressPercentage());
        Assert.Equal(0, project.GetTotalTasksCount());
        Assert.Equal(0, project.GetCompletedTasksCount());
    }

    [Fact]
    public void CalculateProgressPercentage_WithHalfCompletedTasks_ReturnsFifty()
    {
        var project = new Project
        {
            Id = 1,
            Name = "Proyecto 50%",
            Tasks =
            [
                new TaskItem { Status = TaskItemStatus.Completada },
                new TaskItem { Status = TaskItemStatus.Completada },
                new TaskItem { Status = TaskItemStatus.EnProgreso },
                new TaskItem { Status = TaskItemStatus.Pendiente }
            ]
        };

        Assert.Equal(4, project.GetTotalTasksCount());
        Assert.Equal(2, project.GetCompletedTasksCount());
        Assert.Equal(50d, project.CalculateProgressPercentage());
    }

    [Fact]
    public void CalculateProgressPercentage_WithRepeatingDecimal_RoundsToTwoDecimals()
    {
        var project = new Project
        {
            Id = 1,
            Name = "Proyecto 1/3",
            Tasks =
            [
                new TaskItem { Status = TaskItemStatus.Completada },
                new TaskItem { Status = TaskItemStatus.Pendiente },
                new TaskItem { Status = TaskItemStatus.EnProgreso }
            ]
        };

        Assert.Equal(33.33d, project.CalculateProgressPercentage());
    }

    [Fact]
    public void CalculateProgressPercentage_WithAllTasksCompleted_ReturnsHundred()
    {
        var project = new Project
        {
            Id = 1,
            Name = "Proyecto terminado",
            Tasks =
            [
                new TaskItem { Status = TaskItemStatus.Completada },
                new TaskItem { Status = TaskItemStatus.Completada }
            ]
        };

        Assert.Equal(100d, project.CalculateProgressPercentage());
    }

    [Theory]
    [InlineData(TaskItemStatus.Pendiente)]
    [InlineData(TaskItemStatus.EnProgreso)]
    public void EnsureCanBeDeleted_WithIncompleteTask_ThrowsBusinessRuleException(TaskItemStatus status)
    {
        var project = new Project
        {
            Id = 1,
            Name = "Proyecto bloqueado",
            Tasks = [new TaskItem { Status = status }]
        };

        Assert.True(project.HasIncompleteTasks());
        var exception = Assert.Throws<BusinessRuleException>(() => project.EnsureCanBeDeleted());
        Assert.Equal("project_has_incomplete_tasks", exception.ErrorCode);
    }

    [Fact]
    public void EnsureCanBeDeleted_WithoutTasksOrOnlyCompleted_DoesNotThrow()
    {
        var emptyProject = new Project { Id = 1, Name = "Vacío" };
        var completedProject = new Project
        {
            Id = 2,
            Name = "Completado",
            Tasks = [new TaskItem { Status = TaskItemStatus.Completada }]
        };

        Assert.False(emptyProject.HasIncompleteTasks());
        Assert.False(completedProject.HasIncompleteTasks());

        emptyProject.EnsureCanBeDeleted();
        completedProject.EnsureCanBeDeleted();
    }

    [Fact]
    public void UpdateDates_WhenEndDateIsBeforeStartDate_ThrowsBusinessRuleException()
    {
        var project = new Project();
        var start = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<BusinessRuleException>(() => project.UpdateDates(start, end));
    }

    [Fact]
    public void UpdateDates_NormalizesUnspecifiedDatesToUtc()
    {
        var project = new Project();
        var start = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Unspecified);

        project.UpdateDates(start, end);

        Assert.Equal(DateTimeKind.Utc, project.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, project.EndDate.Kind);
        Assert.Equal(9, project.StartDate.Hour);
    }

    [Fact]
    public void NormalizeToUtc_ConvertsLocalTimeToUtc()
    {
        var local = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Local);

        var normalized = Project.NormalizeToUtc(local);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(local.ToUniversalTime(), normalized);
    }
}
