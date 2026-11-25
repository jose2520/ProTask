using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Interfaces;
using ProTask.Infrastructure.Data;

namespace ProTask.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .AsNoTracking()
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetFilteredAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> query = _context.Tasks.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(task => task.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(task => task.Priority == priority.Value);
        }

        return await query
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetOverdueIncompleteAsync(
        DateTime referenceUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedUtc = Project.NormalizeToUtc(referenceUtc);

        return await _context.Tasks
            .AsNoTracking()
            .Where(task => task.Status != TaskItemStatus.Completada && task.DueDate < normalizedUtc)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem> AddAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskItem);

        await _context.Tasks.AddAsync(taskItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return taskItem;
    }

    public async Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskItem);

        if (_context.Entry(taskItem).State == EntityState.Detached)
        {
            _context.Tasks.Attach(taskItem);
            _context.Entry(taskItem).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskItem);

        _context.Tasks.Remove(taskItem);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
