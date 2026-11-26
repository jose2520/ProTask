using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Infrastructure.Repositories;
using ProTask.Infrastructure.Tests.Fixtures;
using Xunit;

namespace ProTask.Infrastructure.Tests.Repositories;

[Collection(InfrastructureCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TaskRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private int _projectAId;
    private int _projectBId;

    public TaskRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
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

        context.Projects.AddRange(projectA, projectB);
        await context.SaveChangesAsync();

        _projectAId = projectA.Id;
        _projectBId = projectB.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsOnlyTasksOfRequestedProject()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new TaskRepository(context);

        await sut.AddAsync(new TaskItem { Title = "A1", ProjectId = _projectAId, DueDate = DateTime.UtcNow.AddDays(1) });
        await sut.AddAsync(new TaskItem { Title = "B1", ProjectId = _projectBId, DueDate = DateTime.UtcNow.AddDays(2) });

        var tasksA = await sut.GetByProjectIdAsync(_projectAId);

        Assert.Single(tasksA);
        Assert.Equal("A1", tasksA[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByStatusPriorityAndCombined()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new TaskRepository(context);

        await sut.AddAsync(new TaskItem { Title = "P-Alta", Status = TaskItemStatus.Pendiente, Priority = TaskPriority.Alta, DueDate = DateTime.UtcNow.AddDays(1), ProjectId = _projectAId });
        await sut.AddAsync(new TaskItem { Title = "P-Baja", Status = TaskItemStatus.Pendiente, Priority = TaskPriority.Baja, DueDate = DateTime.UtcNow.AddDays(2), ProjectId = _projectAId });
        await sut.AddAsync(new TaskItem { Title = "C-Alta", Status = TaskItemStatus.Completada, Priority = TaskPriority.Alta, DueDate = DateTime.UtcNow.AddDays(3), ProjectId = _projectAId });

        var byStatus = await sut.GetFilteredAsync(TaskItemStatus.Pendiente, null);
        var byPriority = await sut.GetFilteredAsync(null, TaskPriority.Alta);
        var combined = await sut.GetFilteredAsync(TaskItemStatus.Pendiente, TaskPriority.Alta);

        Assert.Equal(2, byStatus.Count);
        Assert.Equal(2, byPriority.Count);
        Assert.Single(combined);
        Assert.Equal("P-Alta", combined[0].Title);
    }

    [Fact]
    public async Task GetOverdueIncompleteAsync_ExcludesCompletedAndFutureTasks()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new TaskRepository(context);
        var referenceUtc = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

        await sut.AddAsync(new TaskItem { Title = "Vencida Pendiente", Status = TaskItemStatus.Pendiente, DueDate = referenceUtc.AddDays(-2), ProjectId = _projectAId });
        await sut.AddAsync(new TaskItem { Title = "Vencida EnProgreso", Status = TaskItemStatus.EnProgreso, DueDate = referenceUtc.AddHours(-1), ProjectId = _projectAId });
        await sut.AddAsync(new TaskItem { Title = "Vencida Completada", Status = TaskItemStatus.Completada, DueDate = referenceUtc.AddDays(-5), ProjectId = _projectAId });
        await sut.AddAsync(new TaskItem { Title = "Futura Pendiente", Status = TaskItemStatus.Pendiente, DueDate = referenceUtc.AddDays(2), ProjectId = _projectAId });

        var overdue = await sut.GetOverdueIncompleteAsync(referenceUtc);

        Assert.Equal(2, overdue.Count);
        Assert.Equal(["Vencida Pendiente", "Vencida EnProgreso"], overdue.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_PersistsChangesInPostgreSql()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new TaskRepository(context);

        var created = await sut.AddAsync(new TaskItem
        {
            Title = "Inicial",
            Status = TaskItemStatus.Pendiente,
            Priority = TaskPriority.Baja,
            DueDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            ProjectId = _projectAId
        });

        created.Title = "Editada";
        created.Status = TaskItemStatus.Completada;
        created.Priority = TaskPriority.Critica;
        await sut.UpdateAsync(created);
        context.ChangeTracker.Clear();

        var reloaded = await sut.GetByIdAsync(created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Editada", reloaded.Title);
        Assert.Equal(TaskItemStatus.Completada, reloaded.Status);
        Assert.Equal(TaskPriority.Critica, reloaded.Priority);

        await sut.DeleteAsync(reloaded);
        context.ChangeTracker.Clear();

        Assert.Null(await sut.GetByIdAsync(created.Id));
    }
}
