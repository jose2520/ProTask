using ProTask.Domain.Enums;

namespace ProTask.Application.DTOs.Tasks;

public sealed record UpdateTaskStatusDto
{
    public TaskItemStatus? Status { get; init; }
}
