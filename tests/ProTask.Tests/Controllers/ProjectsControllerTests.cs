using Microsoft.AspNetCore.Mvc;
using Moq;
using ProTask.API.Controllers;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Interfaces;
using Xunit;

namespace ProTask.Tests.Controllers;

public sealed class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _projectServiceMock = new();
    private readonly Mock<ITaskService> _taskServiceMock = new();
    private readonly ProjectsController _sut;

    public ProjectsControllerTests()
    {
        _sut = new ProjectsController(_projectServiceMock.Object, _taskServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProjects()
    {
        _projectServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProjectDto { Id = 1, Name = "Proyecto A", ProgressPercentage = 50 }]);

        var actionResult = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<ProjectDto>>(ok.Value);
        Assert.Single(items);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithProject()
    {
        _projectServiceMock
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = 1, Name = "Proyecto A" });

        var actionResult = await _sut.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var project = Assert.IsType<ProjectDto>(ok.Value);
        Assert.Equal(1, project.Id);
    }

    [Fact]
    public async Task GetTasksByProject_ReturnsOkWithTasks()
    {
        _taskServiceMock
            .Setup(s => s.GetByProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItemDto { Id = 10, Title = "Tarea 1", ProjectId = 1 }]);

        var actionResult = await _sut.GetTasksByProject(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var tasks = Assert.IsAssignableFrom<IReadOnlyList<TaskItemDto>>(ok.Value);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var dto = new CreateProjectDto
        {
            Name = "Nuevo",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        _projectServiceMock
            .Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = 42, Name = "Nuevo" });

        var actionResult = await _sut.Create(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(nameof(ProjectsController.GetById), created.ActionName);
        Assert.Equal(42, created.RouteValues?["id"]);
    }

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedProject()
    {
        var dto = new UpdateProjectDto
        {
            Name = "Editado",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        _projectServiceMock
            .Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectDto { Id = 1, Name = "Editado" });

        var actionResult = await _sut.Update(1, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var project = Assert.IsType<ProjectDto>(ok.Value);
        Assert.Equal("Editado", project.Name);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _projectServiceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
