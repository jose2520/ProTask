using ProTask.Domain.Enums;

namespace ProTask.Application.DTOs.Tasks;

public sealed record UpdateTaskItemDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskItemStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public DateTime DueDate { get; init; }
}
