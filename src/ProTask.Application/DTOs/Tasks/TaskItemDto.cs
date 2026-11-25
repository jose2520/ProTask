using ProTask.Domain.Enums;

namespace ProTask.Application.DTOs.Tasks;

public sealed record TaskItemDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskItemStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateTime DueDate { get; init; }
    public int ProjectId { get; init; }
    public bool IsOverdue { get; init; }
}
