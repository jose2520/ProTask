using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;

namespace ProTask.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Pendiente;
    public TaskPriority Priority { get; private set; } = TaskPriority.Media;
    public DateTime DueDate { get; private set; }

    public int ProjectId { get; private set; }
    public Project? Project { get; private set; }

    private TaskItem()
    {
    }

    public static TaskItem Create(
        string title,
        string? description,
        TaskPriority priority,
        DateTime dueDate,
        int projectId = 0,
        TaskItemStatus status = TaskItemStatus.Pendiente,
        int id = 0)
    {
        var task = new TaskItem
        {
            Id = id,
            ProjectId = projectId
        };

        task.UpdateDetails(title, description, status, priority, dueDate);
        return task;
    }

    public void UpdateDetails(
        string title,
        string? description,
        TaskItemStatus status,
        TaskPriority priority,
        DateTime dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BusinessRuleException(
                "El título de la tarea es obligatorio.",
                "invalid_task_title");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new BusinessRuleException(
                $"La prioridad '{priority}' no es válida para una tarea.",
                "invalid_task_priority");
        }

        ChangeStatus(status);
        Title = title.Trim();
        Description = Project.NormalizeOptionalText(description);
        Priority = priority;
        DueDate = Project.NormalizeToUtc(dueDate);
    }

    public void ChangeStatus(TaskItemStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new BusinessRuleException(
                $"El estado '{newStatus}' no es válido para una tarea.",
                "invalid_task_status");
        }

        Status = newStatus;
    }

    public bool IsOverdue(DateTime referenceUtc)
    {
        var normalizedReference = Project.NormalizeToUtc(referenceUtc);
        var normalizedDueDate = Project.NormalizeToUtc(DueDate);
        return Status != TaskItemStatus.Completada && normalizedDueDate < normalizedReference;
    }
}
