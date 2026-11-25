using System.Data;
using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using ProTask.Domain.Interfaces;
using ProTask.Infrastructure.Data;

namespace ProTask.Infrastructure.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
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

    public async Task DeleteIfNoIncompleteTasksAsync(int id, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                ?? throw new NotFoundException("el proyecto", id);

            var hasIncompleteTasks = await _context.Tasks
                .AnyAsync(
                    task => task.ProjectId == id && task.Status != TaskItemStatus.Completada,
                    cancellationToken);

            if (hasIncompleteTasks)
            {
                throw new BusinessRuleException(
                    $"No se puede eliminar el proyecto '{project.Name}' (ID: {project.Id}) porque tiene tareas pendientes o en progreso.",
                    "project_has_incomplete_tasks");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == id, cancellationToken);
}
