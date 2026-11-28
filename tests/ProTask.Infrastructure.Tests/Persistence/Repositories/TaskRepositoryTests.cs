using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Infrastructure.Persistence.Repositories;
using ProTask.Infrastructure.Tests.Fixtures;
using Xunit;

namespace ProTask.Infrastructure.Tests.Persistence.Repositories;

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
        var projectA = Project.Create(
            "Proyecto A",
            null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        var projectB = Project.Create(
            "Proyecto B",
            null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

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

        await sut.AddAsync(TaskItem.Create("A1", null, TaskPriority.Media, DateTime.UtcNow.AddDays(1), _projectAId));
        await sut.AddAsync(TaskItem.Create("B1", null, TaskPriority.Media, DateTime.UtcNow.AddDays(2), _projectBId));

        var tasksA = await sut.GetByProjectIdAsync(_projectAId);

        Assert.Single(tasksA);
        Assert.Equal("A1", tasksA[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByStatusPriorityAndCombined()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new TaskRepository(context);

        await sut.AddAsync(TaskItem.Create("P-Alta", null, TaskPriority.Alta, DateTime.UtcNow.AddDays(1), _projectAId, TaskItemStatus.Pendiente));
        await sut.AddAsync(TaskItem.Create("P-Baja", null, TaskPriority.Baja, DateTime.UtcNow.AddDays(2), _projectAId, TaskItemStatus.Pendiente));
        await sut.AddAsync(TaskItem.Create("C-Alta", null, TaskPriority.Alta, DateTime.UtcNow.AddDays(3), _projectAId, TaskItemStatus.Completada));

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

        await sut.AddAsync(TaskItem.Create("Vencida Pendiente", null, TaskPriority.Media, referenceUtc.AddDays(-2), _projectAId, TaskItemStatus.Pendiente));
        await sut.AddAsync(TaskItem.Create("Vencida EnProgreso", null, TaskPriority.Media, referenceUtc.AddHours(-1), _projectAId, TaskItemStatus.EnProgreso));
        await sut.AddAsync(TaskItem.Create("Vencida Completada", null, TaskPriority.Media, referenceUtc.AddDays(-5), _projectAId, TaskItemStatus.Completada));
        await sut.AddAsync(TaskItem.Create("Futura Pendiente", null, TaskPriority.Media, referenceUtc.AddDays(2), _projectAId, TaskItemStatus.Pendiente));

        var overdue = await sut.GetOverdueIncompleteAsync(referenceUtc);

        Assert.Equal(2, overdue.Count);
        Assert.Equal(["Vencida Pendiente", "Vencida EnProgreso"], overdue.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_PersistsChangesInPostgreSql()
    {
        await using var context = _fixture.CreateDbContext();
        var sut = new TaskRepository(context);

        var created = await sut.AddAsync(TaskItem.Create(
            "Inicial",
            null,
            TaskPriority.Baja,
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            _projectAId,
            TaskItemStatus.Pendiente));

        created.UpdateDetails("Editada", null, TaskItemStatus.Completada, TaskPriority.Critica, created.DueDate);
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
