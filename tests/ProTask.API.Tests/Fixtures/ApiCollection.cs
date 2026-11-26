using Xunit;

namespace ProTask.API.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiPostgreSqlFixture>
{
    public const string Name = "PostgreSQL API";
}
