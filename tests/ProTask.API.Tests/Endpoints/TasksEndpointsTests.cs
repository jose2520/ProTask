using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Domain.Enums;
using ProTask.API.Tests.Fixtures;
using Xunit;

namespace ProTask.API.Tests.Endpoints;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TasksEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiPostgreSqlFixture _fixture;
    private readonly HttpClient _client;

    public TasksEndpointsTests(ApiPostgreSqlFixture fixture)
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
    public async Task TaskQueries_FiltersOverdueAndByProject_ReturnExpectedResults()
    {
        var project = await CreateSampleProjectAsync("Filtros PostgreSQL");

        var overdueResp = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Vencida Alta",
            Priority = TaskPriority.Alta,
            DueDate = DateTime.UtcNow.AddDays(-3),
            ProjectId = project.Id
        });
        Assert.Equal(HttpStatusCode.Created, overdueResp.StatusCode);

        var completedOverdueResp = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Vencida pero completada",
            Priority = TaskPriority.Alta,
            DueDate = DateTime.UtcNow.AddDays(-4),
            ProjectId = project.Id
        });
        var completedOverdue = await completedOverdueResp.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        Assert.NotNull(completedOverdue);

        await _client.PatchAsJsonAsync(
            $"/api/tasks/{completedOverdue.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.Completada });

        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Futura Baja",
            Priority = TaskPriority.Baja,
            DueDate = DateTime.UtcNow.AddDays(4),
            ProjectId = project.Id
        });

        var overdueTasks = await _client.GetFromJsonAsync<IReadOnlyList<TaskItemDto>>("/api/tasks/overdue", JsonOptions);
        Assert.NotNull(overdueTasks);
        Assert.Single(overdueTasks);
        Assert.Equal("Vencida Alta", overdueTasks[0].Title);
        Assert.True(overdueTasks[0].IsOverdue);

        var filteredTasks = await _client.GetFromJsonAsync<IReadOnlyList<TaskItemDto>>(
            "/api/tasks?status=Pendiente&priority=Alta",
            JsonOptions);
        Assert.NotNull(filteredTasks);
        Assert.Single(filteredTasks);
        Assert.Equal("Vencida Alta", filteredTasks[0].Title);

        var projectTasks = await _client.GetFromJsonAsync<IReadOnlyList<TaskItemDto>>(
            $"/api/projects/{project.Id}/tasks",
            JsonOptions);
        Assert.NotNull(projectTasks);
        Assert.Equal(3, projectTasks.Count);
    }

    [Fact]
    public async Task UpdateAndDeleteTask_WorksEndToEnd()
    {
        var project = await CreateSampleProjectAsync("CRUD Tareas");

        var createTask = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Tarea inicial",
            Priority = TaskPriority.Baja,
            DueDate = DateTime.UtcNow.AddDays(3),
            ProjectId = project.Id
        });
        var task = await createTask.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        Assert.NotNull(task);

        var updateTaskResp = await _client.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskItemDto
        {
            Title = "Tarea editada",
            Description = "Detalle actualizado",
            Status = TaskItemStatus.EnProgreso,
            Priority = TaskPriority.Critica,
            DueDate = DateTime.UtcNow.AddDays(4)
        });
        Assert.Equal(HttpStatusCode.OK, updateTaskResp.StatusCode);
        var updatedTask = await updateTaskResp.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        Assert.Equal("Tarea editada", updatedTask?.Title);
        Assert.Equal(TaskItemStatus.EnProgreso, updatedTask?.Status);
        Assert.Equal(TaskPriority.Critica, updatedTask?.Priority);

        var deleteTaskResp = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTaskResp.StatusCode);

        var getDeletedTask = await _client.GetAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedTask.StatusCode);
    }

    [Fact]
    public async Task UpdateEndpoints_WithOmittedEnumFields_ReturnBadRequestWithoutCorruptingTask()
    {
        var project = await CreateSampleProjectAsync("Protección Enums");

        var createTask = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Tarea crítica",
            Priority = TaskPriority.Critica,
            DueDate = DateTime.UtcNow.AddDays(3),
            ProjectId = project.Id
        });
        var task = await createTask.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        Assert.NotNull(task);

        var completeResp = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.Completada });
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);

        using var emptyPatch = new StringContent("{}", Encoding.UTF8, "application/json");
        var emptyPatchResp = await _client.PatchAsync($"/api/tasks/{task.Id}/status", emptyPatch);
        Assert.Equal(HttpStatusCode.BadRequest, emptyPatchResp.StatusCode);
        Assert.Equal("application/problem+json", emptyPatchResp.Content.Headers.ContentType?.MediaType);

        using var typoPatch = new StringContent("{\"estado\":\"EnProgreso\"}", Encoding.UTF8, "application/json");
        var typoPatchResp = await _client.PatchAsync($"/api/tasks/{task.Id}/status", typoPatch);
        Assert.Equal(HttpStatusCode.BadRequest, typoPatchResp.StatusCode);

        using var missingEnumsPut = new StringContent(
            "{\"title\":\"Intento parcial\",\"dueDate\":\"2026-12-10T00:00:00Z\"}",
            Encoding.UTF8,
            "application/json");
        var missingEnumsPutResp = await _client.PutAsync($"/api/tasks/{task.Id}", missingEnumsPut);
        Assert.Equal(HttpStatusCode.BadRequest, missingEnumsPutResp.StatusCode);

        var intactTask = await _client.GetFromJsonAsync<TaskItemDto>($"/api/tasks/{task.Id}", JsonOptions);
        Assert.NotNull(intactTask);
        Assert.Equal(TaskItemStatus.Completada, intactTask.Status);
        Assert.Equal(TaskPriority.Critica, intactTask.Priority);
        Assert.Equal("Tarea crítica", intactTask.Title);
    }

    [Fact]
    public async Task TaskErrorPaths_MissingProjectAndInvalidFilter_ReturnControlledProblemDetails()
    {
        var missingProjectResp = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Sin proyecto",
            Priority = TaskPriority.Media,
            DueDate = DateTime.UtcNow.AddDays(2),
            ProjectId = 9999
        });
        Assert.Equal(HttpStatusCode.NotFound, missingProjectResp.StatusCode);
        Assert.Equal("application/problem+json", missingProjectResp.Content.Headers.ContentType?.MediaType);

        var invalidFilterResp = await _client.GetAsync("/api/tasks?status=999");
        Assert.Equal(HttpStatusCode.BadRequest, invalidFilterResp.StatusCode);
        Assert.Equal("application/problem+json", invalidFilterResp.Content.Headers.ContentType?.MediaType);
    }

    private async Task<ProjectDto> CreateSampleProjectAsync(string name)
    {
        var resp = await _client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = name,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(10)
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var project = await resp.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.NotNull(project);
        return project;
    }
}
