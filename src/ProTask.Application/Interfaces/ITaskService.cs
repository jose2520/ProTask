using ProTask.Application.DTOs.Tasks;
using ProTask.Domain.Enums;

namespace ProTask.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyList<TaskItemDto>> GetAllAsync(
        TaskItemStatus? status = null,
        TaskPriority? priority = null,
        CancellationToken cancellationToken = default);

    Task<TaskItemDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItemDto>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItemDto>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<TaskItemDto> CreateAsync(CreateTaskItemDto dto, CancellationToken cancellationToken = default);
    Task<TaskItemDto> UpdateAsync(int id, UpdateTaskItemDto dto, CancellationToken cancellationToken = default);
    Task<TaskItemDto> UpdateStatusAsync(int id, UpdateTaskStatusDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
