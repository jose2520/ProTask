using Xunit;

namespace ProTask.Infrastructure.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL Infrastructure";
}
