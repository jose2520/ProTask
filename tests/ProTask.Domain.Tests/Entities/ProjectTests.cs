using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using Xunit;

namespace ProTask.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public sealed class ProjectTests
{
    private static readonly DateTime DefaultStart = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEnd = new(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultDue = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    private static Project CreateProject(string name, int id = 1, params TaskItemStatus[] taskStatuses)
    {
        var project = Project.Create(name, null, DefaultStart, DefaultEnd, id);
        project.AddTasks(taskStatuses.Select((status, index) =>
            TaskItem.Create($"Tarea {index + 1}", null, TaskPriority.Media, DefaultDue, id, status)));
        return project;
    }

    [Fact]
    public void CalculateProgressPercentage_WithoutTasks_ReturnsZero()
    {
        var project = CreateProject("Sin tareas");

        Assert.Equal(0d, project.CalculateProgressPercentage());
        Assert.Equal(0, project.GetTotalTasksCount());
        Assert.Equal(0, project.GetCompletedTasksCount());
    }

    [Fact]
    public void CalculateProgressPercentage_WithHalfCompletedTasks_ReturnsFifty()
    {
        var project = CreateProject(
            "Proyecto 50%",
            1,
            TaskItemStatus.Completada,
            TaskItemStatus.Completada,
            TaskItemStatus.EnProgreso,
            TaskItemStatus.Pendiente);

        Assert.Equal(4, project.GetTotalTasksCount());
        Assert.Equal(2, project.GetCompletedTasksCount());
        Assert.Equal(50d, project.CalculateProgressPercentage());
    }

    [Fact]
    public void CalculateProgressPercentage_WithRepeatingDecimal_RoundsToTwoDecimals()
    {
        var project = CreateProject(
            "Proyecto 1/3",
            1,
            TaskItemStatus.Completada,
            TaskItemStatus.Pendiente,
            TaskItemStatus.EnProgreso);

        Assert.Equal(33.33d, project.CalculateProgressPercentage());
    }

    [Fact]
    public void CalculateProgressPercentage_WithAllTasksCompleted_ReturnsHundred()
    {
        var project = CreateProject(
            "Proyecto terminado",
            1,
            TaskItemStatus.Completada,
            TaskItemStatus.Completada);

        Assert.Equal(100d, project.CalculateProgressPercentage());
    }

    [Theory]
    [InlineData(TaskItemStatus.Pendiente)]
    [InlineData(TaskItemStatus.EnProgreso)]
    public void EnsureCanBeDeleted_WithIncompleteTask_ThrowsBusinessRuleException(TaskItemStatus status)
    {
        var project = CreateProject("Proyecto bloqueado", 1, status);

        Assert.True(project.HasIncompleteTasks());
        var exception = Assert.Throws<BusinessRuleException>(() => project.EnsureCanBeDeleted());
        Assert.Equal(Project.IncompleteTasksErrorCode, exception.ErrorCode);
    }

    [Fact]
    public void EnsureCanBeDeleted_WithoutTasksOrOnlyCompleted_DoesNotThrow()
    {
        var emptyProject = CreateProject("Vacío", 1);
        var completedProject = CreateProject("Completado", 2, TaskItemStatus.Completada);

        Assert.False(emptyProject.HasIncompleteTasks());
        Assert.False(completedProject.HasIncompleteTasks());

        emptyProject.EnsureCanBeDeleted();
        completedProject.EnsureCanBeDeleted();
    }

    [Fact]
    public void UpdateDates_WhenEndDateIsBeforeStartDate_ThrowsBusinessRuleException()
    {
        var project = CreateProject("Fechas");
        var start = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<BusinessRuleException>(() => project.UpdateDates(start, end));
    }

    [Fact]
    public void UpdateDates_NormalizesUnspecifiedDatesToUtc()
    {
        var project = CreateProject("Fechas");
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
