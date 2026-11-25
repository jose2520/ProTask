using FluentValidation;
using Moq;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.Services;
using ProTask.Application.Validators;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using ProTask.Domain.Interfaces;
using Xunit;

namespace ProTask.Tests.Services;

public sealed class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(
            _projectRepositoryMock.Object,
            new CreateProjectValidator(),
            new UpdateProjectValidator());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProjectsWithProgressPercentage()
    {
        var projects = new List<Project>
        {
            new()
            {
                Id = 1,
                Name = "API Core",
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Tasks =
                [
                    new TaskItem { Status = TaskItemStatus.Completada },
                    new TaskItem { Status = TaskItemStatus.Pendiente }
                ]
            },
            new()
            {
                Id = 2,
                Name = "Vacío",
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        _projectRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(50d, result[0].ProgressPercentage);
        Assert.Equal(0d, result[1].ProgressPercentage);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        _projectRepositoryMock
            .Setup(repo => repo.GetByIdWithTasksAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAndReturnsProject()
    {
        var dto = new CreateProjectDto
        {
            Name = "  ProTask  ",
            Description = "Gestión de tareas",
            StartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified),
            EndDate = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Unspecified)
        };

        _projectRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) =>
            {
                p.Id = 10;
                return p;
            });

        var created = await _sut.CreateAsync(dto);

        Assert.Equal(10, created.Id);
        Assert.Equal("ProTask", created.Name);
        Assert.Equal(DateTimeKind.Utc, created.StartDate.Kind);
        Assert.Equal(0d, created.ProgressPercentage);
    }

    [Fact]
    public async Task CreateAsync_WhenEndDateBeforeStartDate_ThrowsValidationException()
    {
        var dto = new CreateProjectDto
        {
            Name = "Fechas inválidas",
            StartDate = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto));
        _projectRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenProjectExists_UpdatesAndReturnsDto()
    {
        var existing = new Project
        {
            Id = 3,
            Name = "Original",
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        _projectRepositoryMock
            .Setup(repo => repo.GetByIdWithTasksAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var dto = new UpdateProjectDto
        {
            Name = "Actualizado",
            Description = "Nueva descripción",
            StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        var updated = await _sut.UpdateAsync(3, dto);

        Assert.Equal("Actualizado", updated.Name);
        Assert.Equal("Nueva descripción", updated.Description);
        _projectRepositoryMock.Verify(repo => repo.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryThrowsBusinessRuleException_PropagatesException()
    {
        _projectRepositoryMock
            .Setup(repo => repo.DeleteIfNoIncompleteTasksAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException("Proyecto con tareas activas", "project_has_incomplete_tasks"));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(5));
        _projectRepositoryMock.Verify(repo => repo.DeleteIfNoIncompleteTasksAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenProjectNotFound_PropagatesNotFoundException()
    {
        _projectRepositoryMock
            .Setup(repo => repo.DeleteIfNoIncompleteTasksAsync(404, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("el proyecto", 404));

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToAtomicRepositoryDelete()
    {
        await _sut.DeleteAsync(6);

        _projectRepositoryMock.Verify(repo => repo.DeleteIfNoIncompleteTasksAsync(6, It.IsAny<CancellationToken>()), Times.Once);
    }
}
