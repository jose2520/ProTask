using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using ProTask.Infrastructure.Data;
using ProTask.Infrastructure.Repositories;
using Xunit;

namespace ProTask.Tests.Integration;

public sealed class ProjectRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private ProjectRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new ProjectRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AddAndGetByIdWithTasksAsync_PersistsRelationOneToMany()
    {
        var project = new Project
        {
            Name = "Backend PostgreSQL",
            Description = "Persistencia relacional",
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Tasks =
            [
                new TaskItem
                {
                    Title = "Crear DbContext",
                    Status = TaskItemStatus.Completada,
                    Priority = TaskPriority.Alta,
                    DueDate = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
                },
                new TaskItem
                {
                    Title = "Crear repositorios",
                    Status = TaskItemStatus.Pendiente,
                    Priority = TaskPriority.Media,
                    DueDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var created = await _sut.AddAsync(project);
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdWithTasksAsync(created.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Backend PostgreSQL", loaded.Name);
        Assert.Equal(2, loaded.Tasks.Count);
        Assert.Equal(50d, loaded.CalculateProgressPercentage());
    }

    [Fact]
    public async Task DeleteAsync_CascadesCompletedTasksAndRemovesProject()
    {
        var project = await _sut.AddAsync(new Project
        {
            Name = "Proyecto borrable",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5),
            Tasks =
            [
                new TaskItem
                {
                    Title = "Terminada",
                    Status = TaskItemStatus.Completada,
                    Priority = TaskPriority.Baja,
                    DueDate = DateTime.UtcNow.AddDays(1)
                }
            ]
        });

        await _sut.DeleteAsync(project);

        Assert.False(await _sut.ExistsAsync(project.Id));
        Assert.Empty(await _context.Tasks.ToListAsync());
    }

    [Theory]
    [InlineData(TaskItemStatus.Pendiente)]
    [InlineData(TaskItemStatus.EnProgreso)]
    public async Task DeleteIfNoIncompleteTasksAsync_WithIncompleteTask_ThrowsAndRollsBackTransaction(TaskItemStatus status)
    {
        var project = await _sut.AddAsync(new Project
        {
            Name = "Proyecto protegido atómicamente",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5),
            Tasks =
            [
                new TaskItem
                {
                    Title = "Activa",
                    Status = status,
                    Priority = TaskPriority.Alta,
                    DueDate = DateTime.UtcNow.AddDays(2)
                }
            ]
        });

        _context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteIfNoIncompleteTasksAsync(project.Id));

        Assert.True(await _sut.ExistsAsync(project.Id));
        Assert.Single(await _context.Tasks.ToListAsync());
    }

    [Fact]
    public async Task DeleteIfNoIncompleteTasksAsync_WithOnlyCompletedTasks_DeletesProjectAndCascades()
    {
        var project = await _sut.AddAsync(new Project
        {
            Name = "Proyecto listo para eliminar",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5),
            Tasks =
            [
                new TaskItem
                {
                    Title = "Completada",
                    Status = TaskItemStatus.Completada,
                    Priority = TaskPriority.Media,
                    DueDate = DateTime.UtcNow.AddDays(1)
                }
            ]
        });

        _context.ChangeTracker.Clear();

        await _sut.DeleteIfNoIncompleteTasksAsync(project.Id);

        Assert.False(await _sut.ExistsAsync(project.Id));
        Assert.Empty(await _context.Tasks.ToListAsync());
    }

    [Fact]
    public async Task DeleteIfNoIncompleteTasksAsync_WhenProjectMissing_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteIfNoIncompleteTasksAsync(9999));
    }
}
