using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using ProTask.Infrastructure.Persistence.Repositories;
using ProTask.Infrastructure.Tests.Fixtures;
using Xunit;

namespace ProTask.Infrastructure.Tests.Persistence.Repositories;

[Collection(InfrastructureCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProjectRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public ProjectRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAndGetByIdWithTasksAsync_PersistsRelationOneToMany()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new ProjectRepository(context);

        var project = Project.Create(
            "Backend PostgreSQL",
            "Persistencia relacional",
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        project.AddTasks([
            TaskItem.Create("Crear DbContext", null, TaskPriority.Alta, new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), status: TaskItemStatus.Completada),
            TaskItem.Create("Crear repositorios", null, TaskPriority.Media, new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), status: TaskItemStatus.Pendiente)
        ]);

        var created = await sut.AddAsync(project);
        context.ChangeTracker.Clear();

        var loaded = await sut.GetByIdWithTasksAsync(created.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Backend PostgreSQL", loaded.Name);
        Assert.Equal(2, loaded.Tasks.Count);
        Assert.Equal(50d, loaded.CalculateProgressPercentage());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTrackedProjectWithoutError()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new ProjectRepository(context);

        var created = await sut.AddAsync(Project.Create(
            "Original",
            null,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        created.UpdateDetails("Actualizado", null, created.StartDate, created.EndDate);
        await sut.UpdateAsync(created);
        context.ChangeTracker.Clear();

        var reloaded = await sut.GetByIdAsync(created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Actualizado", reloaded.Name);
    }

    [Theory]
    [InlineData(TaskItemStatus.Pendiente)]
    [InlineData(TaskItemStatus.EnProgreso)]
    public async Task DeleteIfNoIncompleteTasksAsync_WithIncompleteTask_ThrowsAndKeepsDataIntact(TaskItemStatus status)
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new ProjectRepository(context);

        var project = Project.Create("Proyecto protegido", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(5));
        project.AddTask(TaskItem.Create("Activa", null, TaskPriority.Alta, DateTime.UtcNow.AddDays(2), status: status));

        project = await sut.AddAsync(project);
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.DeleteIfNoIncompleteTasksAsync(project.Id));

        Assert.True(await sut.ExistsAsync(project.Id));
        Assert.Single(await context.Tasks.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task DeleteIfNoIncompleteTasksAsync_WithOnlyCompletedTasks_DeletesProjectAndCascades()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new ProjectRepository(context);

        var project = Project.Create("Proyecto eliminable", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(5));
        project.AddTask(TaskItem.Create("Completada", null, TaskPriority.Media, DateTime.UtcNow.AddDays(1), status: TaskItemStatus.Completada));

        project = await sut.AddAsync(project);
        context.ChangeTracker.Clear();

        var deleted = await sut.DeleteIfNoIncompleteTasksAsync(project.Id);

        Assert.True(deleted);
        Assert.False(await sut.ExistsAsync(project.Id));
        Assert.Empty(await context.Tasks.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task DeleteIfNoIncompleteTasksAsync_WhenProjectMissing_ReturnsFalse()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new ProjectRepository(context);

        var deleted = await sut.DeleteIfNoIncompleteTasksAsync(9999);

        Assert.False(deleted);
    }
}
