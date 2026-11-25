using ProTask.Domain.Enums;

namespace ProTask.Application.DTOs.Tasks;

public sealed record CreateTaskItemDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.Media;
    public DateTime DueDate { get; init; }
    public int ProjectId { get; init; }
}
