using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProTask.Infrastructure.Data;

namespace ProTask.API.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Conexión exitosa a PostgreSQL.")
                : HealthCheckResult.Unhealthy("No se pudo conectar con PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al verificar la conexión con PostgreSQL.", ex);
        }
    }
}
