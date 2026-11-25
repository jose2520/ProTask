using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;

namespace ProTask.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public int GetTotalTasksCount() => Tasks.Count;

    public int GetCompletedTasksCount() =>
        Tasks.Count(task => task.Status == TaskItemStatus.Completada);

    public double CalculateProgressPercentage()
    {
        if (Tasks.Count == 0)
        {
            return 0d;
        }

        var completedCount = GetCompletedTasksCount();
        return Math.Round((double)completedCount / Tasks.Count * 100d, 2, MidpointRounding.AwayFromZero);
    }

    public bool HasIncompleteTasks() =>
        Tasks.Any(task => task.Status != TaskItemStatus.Completada);

    public void EnsureCanBeDeleted()
    {
        if (HasIncompleteTasks())
        {
            throw new BusinessRuleException(
                $"No se puede eliminar el proyecto '{Name}' (ID: {Id}) porque tiene tareas pendientes o en progreso.",
                "project_has_incomplete_tasks");
        }
    }

    public void UpdateDates(DateTime startDate, DateTime endDate)
    {
        var normalizedStart = NormalizeToUtc(startDate);
        var normalizedEnd = NormalizeToUtc(endDate);

        if (normalizedEnd < normalizedStart)
        {
            throw new BusinessRuleException(
                "La fecha de finalización no puede ser anterior a la fecha de inicio.",
                "invalid_project_dates");
        }

        StartDate = normalizedStart;
        EndDate = normalizedEnd;
    }

    public static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
