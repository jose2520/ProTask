using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ProTask.Infrastructure.Data;
using Xunit;

namespace ProTask.Infrastructure.Tests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public const string InfraTestConnectionEnvVar = "PROTASK_INFRA_TEST_CONNECTION";
    public const string SharedTestConnectionEnvVar = "PROTASK_TEST_CONNECTION";
    private const string ApiUserSecretsId = "e7d9e932-f51e-4eb9-a543-ba68c5ad5c4d";
    private const string TestDatabaseName = "protask_infra_test";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString = ResolveTestConnectionString();

        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            await ResetDatabaseAsync();
        }
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: ["40001"]);
                npgsql.CommandTimeout(30);
            })
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE tasks, projects RESTART IDENTITY CASCADE;");
    }

    private static string ResolveTestConnectionString()
    {
        var specific = Environment.GetEnvironmentVariable(InfraTestConnectionEnvVar);
        if (!string.IsNullOrWhiteSpace(specific))
        {
            return EnsureSafeTestDatabase(specific, overrideDatabaseName: false);
        }

        var shared = Environment.GetEnvironmentVariable(SharedTestConnectionEnvVar);
        if (!string.IsNullOrWhiteSpace(shared))
        {
            return EnsureSafeTestDatabase(shared, overrideDatabaseName: true);
        }

        var config = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .Build();

        var apiConnection = config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(apiConnection))
        {
            return EnsureSafeTestDatabase(apiConnection, overrideDatabaseName: true);
        }

        throw new InvalidOperationException(
            $"No se encontró cadena de conexión para ProTask.Infrastructure.Tests. " +
            $"Configura '{InfraTestConnectionEnvVar}', '{SharedTestConnectionEnvVar}' o 'ConnectionStrings:DefaultConnection' en dotnet user-secrets de ProTask.API.");
    }

    private static string EnsureSafeTestDatabase(string rawConnectionString, bool overrideDatabaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString);

        if (overrideDatabaseName)
        {
            builder.Database = TestDatabaseName;
        }

        if (string.IsNullOrWhiteSpace(builder.Database)
            || !builder.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Por seguridad, las pruebas de integración solo pueden ejecutarse contra una base de datos terminada en '_test' (actual: '{builder.Database}').");
        }

        return builder.ConnectionString;
    }
}
