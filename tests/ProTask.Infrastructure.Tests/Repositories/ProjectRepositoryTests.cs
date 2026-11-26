using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using ProTask.Infrastructure.Repositories;
using ProTask.Infrastructure.Tests.Fixtures;
using Xunit;

namespace ProTask.Infrastructure.Tests.Repositories;

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

        var created = await sut.AddAsync(new Project
        {
            Name = "Original",
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        created.Name = "Actualizado";
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

        var project = await sut.AddAsync(new Project
        {
            Name = "Proyecto protegido",
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

        var project = await sut.AddAsync(new Project
        {
            Name = "Proyecto eliminable",
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

        context.ChangeTracker.Clear();

        await sut.DeleteIfNoIncompleteTasksAsync(project.Id);

        Assert.False(await sut.ExistsAsync(project.Id));
        Assert.Empty(await context.Tasks.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task DeleteIfNoIncompleteTasksAsync_WhenProjectMissing_ThrowsNotFoundException()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new ProjectRepository(context);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteIfNoIncompleteTasksAsync(9999));
    }
}
