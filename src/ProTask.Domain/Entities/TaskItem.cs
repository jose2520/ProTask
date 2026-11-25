using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;

namespace ProTask.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pendiente;
    public TaskPriority Priority { get; set; } = TaskPriority.Media;
    public DateTime DueDate { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

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

    public bool IsOverdue() => IsOverdue(DateTime.UtcNow);
}
