using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;

namespace ProTask.Domain.Entities;

public class Project
{
    public const string IncompleteTasksErrorCode = "project_has_incomplete_tasks";

    private readonly List<TaskItem> _tasks = [];

    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    private Project()
    {
    }

    public static Project Create(
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        int id = 0)
    {
        var project = new Project { Id = id };
        project.UpdateDetails(name, description, startDate, endDate);
        return project;
    }

    public void UpdateDetails(string name, string? description, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException(
                "El nombre del proyecto es obligatorio.",
                "invalid_project_name");
        }

        UpdateDates(startDate, endDate);
        Name = name.Trim();
        Description = NormalizeOptionalText(description);
    }

    public void AddTask(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _tasks.Add(task);
    }

    public void AddTasks(IEnumerable<TaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        foreach (var task in tasks)
        {
            AddTask(task);
        }
    }

    public int GetTotalTasksCount() => _tasks.Count;

    public int GetCompletedTasksCount() =>
        _tasks.Count(task => task.Status == TaskItemStatus.Completada);

    public double CalculateProgressPercentage()
    {
        if (_tasks.Count == 0)
        {
            return 0d;
        }

        var completedCount = GetCompletedTasksCount();
        return Math.Round((double)completedCount / _tasks.Count * 100d, 2, MidpointRounding.AwayFromZero);
    }

    public bool HasIncompleteTasks() =>
        _tasks.Any(task => task.Status != TaskItemStatus.Completada);

    public void EnsureCanBeDeleted() => EnsureCanBeDeleted(HasIncompleteTasks());

    public void EnsureCanBeDeleted(bool hasIncompleteTasks)
    {
        if (hasIncompleteTasks)
        {
            throw new BusinessRuleException(
                $"No se puede eliminar el proyecto '{Name}' (ID: {Id}) porque tiene tareas pendientes o en progreso.",
                IncompleteTasksErrorCode);
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

    internal static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
