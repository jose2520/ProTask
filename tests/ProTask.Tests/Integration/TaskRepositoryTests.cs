using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Infrastructure.Data;
using ProTask.Infrastructure.Repositories;
using Xunit;

namespace ProTask.Tests.Integration;

public sealed class TaskRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private TaskRepository _sut = null!;
    private int _projectAId;
    private int _projectBId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var projectA = new Project
        {
            Name = "Proyecto A",
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        };
        var projectB = new Project
        {
            Name = "Proyecto B",
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        };

        _context.Projects.AddRange(projectA, projectB);
        await _context.SaveChangesAsync();

        _projectAId = projectA.Id;
        _projectBId = projectB.Id;
        _sut = new TaskRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsOnlyTasksOfRequestedProject()
    {
        await _sut.AddAsync(new TaskItem { Title = "A1", ProjectId = _projectAId, DueDate = DateTime.UtcNow.AddDays(1) });
        await _sut.AddAsync(new TaskItem { Title = "B1", ProjectId = _projectBId, DueDate = DateTime.UtcNow.AddDays(2) });

        var tasksA = await _sut.GetByProjectIdAsync(_projectAId);

        Assert.Single(tasksA);
        Assert.Equal("A1", tasksA[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByStatusPriorityAndCombined()
    {
        await _sut.AddAsync(new TaskItem { Title = "P-Alta", Status = TaskItemStatus.Pendiente, Priority = TaskPriority.Alta, DueDate = DateTime.UtcNow.AddDays(1), ProjectId = _projectAId });
        await _sut.AddAsync(new TaskItem { Title = "P-Baja", Status = TaskItemStatus.Pendiente, Priority = TaskPriority.Baja, DueDate = DateTime.UtcNow.AddDays(2), ProjectId = _projectAId });
        await _sut.AddAsync(new TaskItem { Title = "C-Alta", Status = TaskItemStatus.Completada, Priority = TaskPriority.Alta, DueDate = DateTime.UtcNow.AddDays(3), ProjectId = _projectAId });

        var byStatus = await _sut.GetFilteredAsync(TaskItemStatus.Pendiente, null);
        var byPriority = await _sut.GetFilteredAsync(null, TaskPriority.Alta);
        var combined = await _sut.GetFilteredAsync(TaskItemStatus.Pendiente, TaskPriority.Alta);

        Assert.Equal(2, byStatus.Count);
        Assert.Equal(2, byPriority.Count);
        Assert.Single(combined);
        Assert.Equal("P-Alta", combined[0].Title);
    }

    [Fact]
    public async Task GetOverdueIncompleteAsync_ExcludesCompletedAndFutureTasks()
    {
        var referenceUtc = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

        await _sut.AddAsync(new TaskItem { Title = "Vencida Pendiente", Status = TaskItemStatus.Pendiente, DueDate = referenceUtc.AddDays(-2), ProjectId = _projectAId });
        await _sut.AddAsync(new TaskItem { Title = "Vencida EnProgreso", Status = TaskItemStatus.EnProgreso, DueDate = referenceUtc.AddHours(-1), ProjectId = _projectAId });
        await _sut.AddAsync(new TaskItem { Title = "Vencida Completada", Status = TaskItemStatus.Completada, DueDate = referenceUtc.AddDays(-5), ProjectId = _projectAId });
        await _sut.AddAsync(new TaskItem { Title = "Futura Pendiente", Status = TaskItemStatus.Pendiente, DueDate = referenceUtc.AddDays(2), ProjectId = _projectAId });

        var overdue = await _sut.GetOverdueIncompleteAsync(referenceUtc);

        Assert.Equal(2, overdue.Count);
        Assert.Equal(["Vencida Pendiente", "Vencida EnProgreso"], overdue.Select(t => t.Title).ToArray());
    }
}
