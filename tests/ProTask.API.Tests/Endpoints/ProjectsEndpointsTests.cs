using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Domain.Enums;
using ProTask.API.Tests.Fixtures;
using Xunit;

namespace ProTask.API.Tests.Endpoints;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProjectsEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiPostgreSqlFixture _fixture;
    private readonly HttpClient _client;

    public ProjectsEndpointsTests(ApiPostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiFactory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FullLifecycle_ProgressCalculationAndDeleteProtection_WorksEndToEnd()
    {
        var createProjectResponse = await _client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Lanzamiento ProTask",
            Description = "API REST Clean Architecture",
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(15)
        });

        Assert.Equal(HttpStatusCode.Created, createProjectResponse.StatusCode);
        var project = await createProjectResponse.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.NotNull(project);
        Assert.Equal(0d, project.ProgressPercentage);

        var task1Response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Diseñar esquema",
            Priority = TaskPriority.Alta,
            DueDate = DateTime.UtcNow.AddDays(-2),
            ProjectId = project.Id
        });
        Assert.Equal(HttpStatusCode.Created, task1Response.StatusCode);
        var task1 = await task1Response.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        Assert.NotNull(task1);

        var task2Response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Escribir pruebas",
            Priority = TaskPriority.Media,
            DueDate = DateTime.UtcNow.AddDays(5),
            ProjectId = project.Id
        });
        Assert.Equal(HttpStatusCode.Created, task2Response.StatusCode);
        var task2 = await task2Response.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        Assert.NotNull(task2);

        var patchTask1 = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task1.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.Completada });
        Assert.Equal(HttpStatusCode.OK, patchTask1.StatusCode);

        var projectAfterOneCompleted = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", JsonOptions);
        Assert.NotNull(projectAfterOneCompleted);
        Assert.Equal(2, projectAfterOneCompleted.TotalTasks);
        Assert.Equal(1, projectAfterOneCompleted.CompletedTasks);
        Assert.Equal(50d, projectAfterOneCompleted.ProgressPercentage);

        var blockedPendingDelete = await _client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blockedPendingDelete.StatusCode);
        Assert.Equal("application/problem+json", blockedPendingDelete.Content.Headers.ContentType?.MediaType);
        var conflictProblem = await blockedPendingDelete.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(conflictProblem);
        Assert.Equal(409, conflictProblem.Status);

        var patchTask2InProgress = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task2.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.EnProgreso });
        Assert.Equal(HttpStatusCode.OK, patchTask2InProgress.StatusCode);

        var blockedInProgressDelete = await _client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blockedInProgressDelete.StatusCode);

        var patchTask2Done = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task2.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.Completada });
        Assert.Equal(HttpStatusCode.OK, patchTask2Done.StatusCode);

        var projectCompleted = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", JsonOptions);
        Assert.NotNull(projectCompleted);
        Assert.Equal(100d, projectCompleted.ProgressPercentage);

        var deleteOk = await _client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteOk.StatusCode);

        var getDeleted = await _client.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
    }

    [Fact]
    public async Task GetAllUpdateAndGetTasksByProject_WorksAgainstPostgreSql()
    {
        using var unspecifiedDatesBody = new StringContent(
            "{\"name\":\"Proyecto Unspecified\",\"description\":\"Fechas sin Z\",\"startDate\":\"2026-11-01T10:00:00\",\"endDate\":\"2026-12-01T18:00:00\"}",
            Encoding.UTF8,
            "application/json");
        var createResp = await _client.PostAsync("/api/projects", unspecifiedDatesBody);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.NotNull(created);

        var updateResp = await _client.PutAsJsonAsync($"/api/projects/{created.Id}", new UpdateProjectDto
        {
            Name = "Proyecto Renombrado",
            Description = "Actualizado",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10)
        });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var allProjects = await _client.GetFromJsonAsync<IReadOnlyList<ProjectDto>>("/api/projects", JsonOptions);
        Assert.NotNull(allProjects);
        Assert.Single(allProjects);
        Assert.Equal("Proyecto Renombrado", allProjects[0].Name);

        var missingTasksResp = await _client.GetAsync("/api/projects/9999/tasks");
        Assert.Equal(HttpStatusCode.NotFound, missingTasksResp.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithInvalidDates_ReturnsValidationProblemDetails()
    {
        var invalidResp = await _client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Fechas invertidas",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidResp.StatusCode);
        Assert.Equal("application/problem+json", invalidResp.Content.Headers.ContentType?.MediaType);
    }
}
