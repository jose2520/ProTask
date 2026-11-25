using ProTask.Domain.Entities;
using ProTask.Domain.Enums;

namespace ProTask.Domain.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetFilteredAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetOverdueIncompleteAsync(
        DateTime referenceUtc,
        CancellationToken cancellationToken = default);
    Task<TaskItem> AddAsync(TaskItem taskItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskItem taskItem, CancellationToken cancellationToken = default);
}
