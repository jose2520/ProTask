using Microsoft.EntityFrameworkCore;
using ProTask.Domain.Entities;
using ProTask.Infrastructure.Tests.Fixtures;
using Xunit;

namespace ProTask.Infrastructure.Tests.Data;

[Collection(InfrastructureCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MigrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Database_HasInitialCreateAppliedAndNoPendingMigrations()
    {
        await using var context = _fixture.CreateDbContext();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Contains(applied, id => id.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Empty(pending);
    }

    [Fact]
    public async Task PostgreSql_EnforcesMaxLengthConstraintOnProjectName()
    {
        await using var context = _fixture.CreateDbContext();

        context.Projects.Add(new Project
        {
            Name = new string('X', 151),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task PostgreSql_PersistsAndReadsUtcTimestamps()
    {
        var startUtc = new DateTime(2026, 11, 1, 10, 30, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2026, 12, 1, 18, 45, 0, DateTimeKind.Utc);

        int id;
        await using (var writeContext = _fixture.CreateDbContext())
        {
            var project = new Project
            {
                Name = "UTC Check",
                StartDate = startUtc,
                EndDate = endUtc
            };
            writeContext.Projects.Add(project);
            await writeContext.SaveChangesAsync();
            id = project.Id;
        }

        await using var readContext = _fixture.CreateDbContext();
        var loaded = await readContext.Projects.AsNoTracking().SingleAsync(p => p.Id == id);

        Assert.Equal(DateTimeKind.Utc, loaded.StartDate.Kind);
        Assert.Equal(startUtc, loaded.StartDate);
        Assert.Equal(endUtc, loaded.EndDate);
    }
}
