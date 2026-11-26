using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProTask.API.Tests.Fixtures;

public sealed class ProTaskApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ProTaskApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Swagger:Enabled", "true");
    }
}
