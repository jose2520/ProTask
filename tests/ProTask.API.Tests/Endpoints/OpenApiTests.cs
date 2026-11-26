using System.Net;
using System.Text.Json;
using ProTask.API.Tests.Fixtures;
using Xunit;

namespace ProTask.API.Tests.Endpoints;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class OpenApiTests : IDisposable
{
    private readonly HttpClient _client;

    public OpenApiTests(ApiPostgreSqlFixture fixture)
    {
        _client = fixture.ApiFactory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task SwaggerUiEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_ExposesAllExpectedPathsAndSchemas()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        string[] expectedPaths =
        [
            "/api/projects",
            "/api/projects/{id}",
            "/api/projects/{id}/tasks",
            "/api/tasks",
            "/api/tasks/overdue",
            "/api/tasks/{id}",
            "/api/tasks/{id}/status"
        ];

        foreach (var expectedPath in expectedPaths)
        {
            Assert.True(paths.TryGetProperty(expectedPath, out _), $"Falta la ruta '{expectedPath}' en /openapi/v1.json.");
        }

        Assert.True(schemas.TryGetProperty("ProjectDto", out _));
        Assert.True(schemas.TryGetProperty("TaskItemDto", out _));
        Assert.True(schemas.TryGetProperty("TaskItemStatus", out _));
        Assert.True(schemas.TryGetProperty("TaskPriority", out _));
    }
}
