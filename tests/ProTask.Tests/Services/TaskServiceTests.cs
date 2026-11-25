using FluentValidation;
using Moq;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Services;
using ProTask.Application.Validators;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using ProTask.Domain.Interfaces;
using Xunit;

namespace ProTask.Tests.Services;

public sealed class TaskServiceTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITaskRepository> _taskRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object,
            new CreateTaskItemValidator(),
            new UpdateTaskItemValidator(),
            new UpdateTaskStatusValidator(),
            new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilters_CallsGetAllOnRepository()
    {
        _taskRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItem { Id = 1, Title = "T1", DueDate = FixedUtcNow.UtcDateTime.AddDays(1) }]);

        var tasks = await _sut.GetAllAsync();

        Assert.Single(tasks);
        Assert.False(tasks[0].IsOverdue);
        _taskRepositoryMock.Verify(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_CallsGetFilteredOnRepository()
    {
        _taskRepositoryMock
            .Setup(repo => repo.GetFilteredAsync(TaskItemStatus.Pendiente, TaskPriority.Alta, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItem { Id = 2, Title = "Urgente", Status = TaskItemStatus.Pendiente, Priority = TaskPriority.Alta, DueDate = FixedUtcNow.UtcDateTime.AddDays(-1) }]);

        var tasks = await _sut.GetAllAsync(TaskItemStatus.Pendiente, TaskPriority.Alta);

        Assert.Single(tasks);
        Assert.True(tasks[0].IsOverdue);
        _taskRepositoryMock.Verify(
            repo => repo.GetFilteredAsync(TaskItemStatus.Pendiente, TaskPriority.Alta, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenProjectDoesNotExist_ThrowsNotFoundException()
    {
        _projectRepositoryMock
            .Setup(repo => repo.ExistsAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByProjectIdAsync(404));
    }

    [Fact]
    public async Task GetOverdueAsync_ReturnsOverdueIncompleteTasks()
    {
        _taskRepositoryMock
            .Setup(repo => repo.GetOverdueIncompleteAsync(FixedUtcNow.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskItem { Id = 7, Title = "Vencida", Status = TaskItemStatus.EnProgreso, DueDate = FixedUtcNow.UtcDateTime.AddHours(-2) }]);

        var result = await _sut.GetOverdueAsync();

        Assert.Single(result);
        Assert.True(result[0].IsOverdue);
    }

    [Fact]
    public async Task CreateAsync_WhenProjectDoesNotExist_ThrowsNotFoundException()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "Tarea huérfana",
            Priority = TaskPriority.Alta,
            DueDate = FixedUtcNow.UtcDateTime.AddDays(3),
            ProjectId = 999
        };

        _projectRepositoryMock
            .Setup(repo => repo.ExistsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(dto));
        _taskRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDto_ThrowsValidationException()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "",
            Priority = TaskPriority.Media,
            DueDate = default,
            ProjectId = 0
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesTaskWithPendienteStatus()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "Configurar migraciones",
            Priority = TaskPriority.Alta,
            DueDate = FixedUtcNow.UtcDateTime.AddDays(2),
            ProjectId = 1
        };

        _projectRepositoryMock
            .Setup(repo => repo.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem t, CancellationToken _) =>
            {
                t.Id = 15;
                return t;
            });

        var created = await _sut.CreateAsync(dto);

        Assert.Equal(15, created.Id);
        Assert.Equal(TaskItemStatus.Pendiente, created.Status);
        Assert.Equal(TaskPriority.Alta, created.Priority);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesOnlyTaskStatus()
    {
        var existing = new TaskItem
        {
            Id = 4,
            Title = "Revisar PR",
            Status = TaskItemStatus.Pendiente,
            Priority = TaskPriority.Media,
            DueDate = FixedUtcNow.UtcDateTime.AddDays(1),
            ProjectId = 1
        };

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var updated = await _sut.UpdateStatusAsync(4, new UpdateTaskStatusDto { Status = TaskItemStatus.Completada });

        Assert.Equal(TaskItemStatus.Completada, updated.Status);
        Assert.Equal("Revisar PR", updated.Title);
        _taskRepositoryMock.Verify(repo => repo.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusIsMissing_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UpdateStatusAsync(4, new UpdateTaskStatusDto { Status = null }));

        _taskRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusOrPriorityIsMissing_ThrowsValidationException()
    {
        var dto = new UpdateTaskItemDto
        {
            Title = "Sin enums",
            Status = null,
            Priority = null,
            DueDate = FixedUtcNow.UtcDateTime.AddDays(1)
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.UpdateAsync(4, dto));
        _taskRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskMissing_ThrowsNotFoundException()
    {
        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(123));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
