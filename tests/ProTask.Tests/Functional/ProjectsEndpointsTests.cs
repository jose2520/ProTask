using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Domain.Enums;
using ProTask.Infrastructure.Data;
using Xunit;

namespace ProTask.Tests.Functional;

public sealed class ProjectsEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private ProTaskWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ProTaskWebApplicationFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task FullLifecycle_ProgressCalculationAndDeleteProtection_WorksEndToEnd()
    {
        // 1. Crear proyecto sin tareas -> 0% de avance
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

        // 2. Crear 2 tareas para el proyecto
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

        // 3. Completar tarea 1 -> avance debe ser 50%
        var patchTask1 = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task1.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.Completada });
        Assert.Equal(HttpStatusCode.OK, patchTask1.StatusCode);

        var projectAfterOneCompleted = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", JsonOptions);
        Assert.NotNull(projectAfterOneCompleted);
        Assert.Equal(2, projectAfterOneCompleted.TotalTasks);
        Assert.Equal(1, projectAfterOneCompleted.CompletedTasks);
        Assert.Equal(50d, projectAfterOneCompleted.ProgressPercentage);

        // 4. Intentar eliminar proyecto con tarea Pendiente -> 409 Conflict (RFC 7807)
        var blockedPendingDelete = await _client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blockedPendingDelete.StatusCode);
        Assert.Equal("application/problem+json", blockedPendingDelete.Content.Headers.ContentType?.MediaType);
        var conflictProblem = await blockedPendingDelete.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(conflictProblem);
        Assert.Equal(409, conflictProblem.Status);

        // 5. Cambiar tarea 2 a EnProgreso -> seguir bloqueando borrado con 409 Conflict
        var patchTask2InProgress = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task2.Id}/status",
            new UpdateTaskStatusDto { Status = TaskItemStatus.EnProgreso });
        Assert.Equal(HttpStatusCode.OK, patchTask2InProgress.StatusCode);

        var blockedInProgressDelete = await _client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blockedInProgressDelete.StatusCode);

        // 6. Completar tarea 2 -> 100% avance y permitir borrado (204 No Content)
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
    public async Task TaskQueries_FiltersOverdueAndByProject_ReturnExpectedResults()
    {
        var projectResp = await _client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Filtros",
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(10)
        });
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.NotNull(project);

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
    public async Task ErrorHandling_InvalidProjectDatesAndMissingProjectId_ReturnsControlledProblemDetails()
    {
        var invalidProjectResp = await _client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Fechas invertidas",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidProjectResp.StatusCode);
        Assert.Equal("application/problem+json", invalidProjectResp.Content.Headers.ContentType?.MediaType);

        var missingProjectTaskResp = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Sin proyecto",
            Priority = TaskPriority.Media,
            DueDate = DateTime.UtcNow.AddDays(2),
            ProjectId = 9999
        });

        Assert.Equal(HttpStatusCode.NotFound, missingProjectTaskResp.StatusCode);
        Assert.Equal("application/problem+json", missingProjectTaskResp.Content.Headers.ContentType?.MediaType);

        var invalidFilterResp = await _client.GetAsync("/api/tasks?status=999");
        Assert.Equal(HttpStatusCode.BadRequest, invalidFilterResp.StatusCode);
        Assert.Equal("application/problem+json", invalidFilterResp.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UpdateAndDeleteTaskAndProject_EndpointsReturnExpectedStatusCodes()
    {
        var createProject = await _client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Inicial",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10)
        });
        var project = await createProject.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.NotNull(project);

        var updateProjectResp = await _client.PutAsJsonAsync($"/api/projects/{project.Id}", new UpdateProjectDto
        {
            Name = "Renombrado",
            Description = "Actualizado",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(20)
        });
        Assert.Equal(HttpStatusCode.OK, updateProjectResp.StatusCode);
        var updatedProject = await updateProjectResp.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
        Assert.Equal("Renombrado", updatedProject?.Name);

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
    public async Task HealthAndOpenApiEndpoints_ReturnOk()
    {
        var healthResponse = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var openApiResponse = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);

        var swaggerUiResponse = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, swaggerUiResponse.StatusCode);
    }

    private sealed class ProTaskWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection;

        public ProTaskWebApplicationFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=protask_testing;Username=postgres");
            builder.UseSetting("Swagger:Enabled", "true");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(_connection));

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
