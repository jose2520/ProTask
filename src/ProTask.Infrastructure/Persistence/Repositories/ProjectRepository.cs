using System.Data;
using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Abstractions;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Infrastructure.Persistence;

namespace ProTask.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Projects
            .AsNoTracking()
            .Include(project => project.Tasks)
            .OrderBy(project => project.Id)
            .ToListAsync(cancellationToken);

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Projects
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

    public async Task<Project?> GetByIdWithTasksAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Projects
            .Include(project => project.Tasks)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        await _context.Projects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (_context.Entry(project).State == EntityState.Detached)
        {
            _context.Projects.Attach(project);
            _context.Entry(project).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteIfNoIncompleteTasksAsync(int id, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (project is null)
            {
                return false;
            }

            var hasIncompleteTasks = await _context.Tasks
                .AnyAsync(
                    task => task.ProjectId == id && task.Status != TaskItemStatus.Completada,
                    cancellationToken);

            project.EnsureCanBeDeleted(hasIncompleteTasks);

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == id, cancellationToken);
}
