using Microsoft.AspNetCore.Mvc;
using Moq;
using ProTask.API.Controllers;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Interfaces;
using ProTask.Domain.Enums;
using Xunit;

namespace ProTask.Tests.Controllers;

public sealed class TasksControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock = new();
    private readonly TasksController _sut;

    public TasksControllerTests()
    {
        _sut = new TasksController(_taskServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_PassesFiltersAndReturnsOk()
    {
        _taskServiceMock
            .Setup(s => s.GetAllAsync(TaskItemStatus.Pendiente, TaskPriority.Alta, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItemDto { Id = 1, Title = "Urgente", Status = TaskItemStatus.Pendiente, Priority = TaskPriority.Alta }]);

        var actionResult = await _sut.GetAll(TaskItemStatus.Pendiente, TaskPriority.Alta, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var tasks = Assert.IsAssignableFrom<IReadOnlyList<TaskItemDto>>(ok.Value);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task GetOverdue_ReturnsOkWithOverdueTasks()
    {
        _taskServiceMock
            .Setup(s => s.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItemDto { Id = 2, Title = "Atrasada", IsOverdue = true }]);

        var actionResult = await _sut.GetOverdue(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var tasks = Assert.IsAssignableFrom<IReadOnlyList<TaskItemDto>>(ok.Value);
        Assert.Single(tasks);
        Assert.True(tasks[0].IsOverdue);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "Nueva tarea",
            Priority = TaskPriority.Media,
            DueDate = DateTime.UtcNow.AddDays(3),
            ProjectId = 1
        };

        _taskServiceMock
            .Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItemDto { Id = 7, Title = "Nueva tarea", ProjectId = 1 });

        var actionResult = await _sut.Create(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(nameof(TasksController.GetById), created.ActionName);
        Assert.Equal(7, created.RouteValues?["id"]);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOkWithUpdatedTask()
    {
        var dto = new UpdateTaskStatusDto { Status = TaskItemStatus.Completada };
        _taskServiceMock
            .Setup(s => s.UpdateStatusAsync(5, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItemDto { Id = 5, Status = TaskItemStatus.Completada });

        var actionResult = await _sut.UpdateStatus(5, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var task = Assert.IsType<TaskItemDto>(ok.Value);
        Assert.Equal(TaskItemStatus.Completada, task.Status);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var result = await _sut.Delete(5, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _taskServiceMock.Verify(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
