using FluentValidation;
using FluentValidation.Results;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Exceptions;
using ProTask.Application.Interfaces;
using ProTask.Application.Mappings;
using ProTask.Domain.Abstractions;
using ProTask.Domain.Enums;

namespace ProTask.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IValidator<CreateTaskItemDto> _createValidator;
    private readonly IValidator<UpdateTaskItemDto> _updateValidator;
    private readonly IValidator<UpdateTaskStatusDto> _updateStatusValidator;
    private readonly TimeProvider _timeProvider;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IValidator<CreateTaskItemDto> createValidator,
        IValidator<UpdateTaskItemDto> updateValidator,
        IValidator<UpdateTaskStatusDto> updateStatusValidator,
        TimeProvider? timeProvider = null)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _updateStatusValidator = updateStatusValidator;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<TaskItemDto>> GetAllAsync(
        TaskItemStatus? status = null,
        TaskPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ValidationException([new ValidationFailure("status", $"El estado '{status}' no es válido.")]);
        }

        if (priority.HasValue && !Enum.IsDefined(priority.Value))
        {
            throw new ValidationException([new ValidationFailure("priority", $"La prioridad '{priority}' no es válida.")]);
        }

        var tasks = status.HasValue || priority.HasValue
            ? await _taskRepository.GetFilteredAsync(status, priority, cancellationToken)
            : await _taskRepository.GetAllAsync(cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return tasks.Select(task => task.ToDto(nowUtc)).ToList();
    }

    public async Task<TaskItemDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("la tarea", id);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return task.ToDto(nowUtc);
    }

    public async Task<IReadOnlyList<TaskItemDto>> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var projectExists = await _projectRepository.ExistsAsync(projectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException("el proyecto", projectId);
        }

        var tasks = await _taskRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return tasks.Select(task => task.ToDto(nowUtc)).ToList();
    }

    public async Task<IReadOnlyList<TaskItemDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var tasks = await _taskRepository.GetOverdueIncompleteAsync(nowUtc, cancellationToken);
        return tasks.Select(task => task.ToDto(nowUtc)).ToList();
    }

    public async Task<TaskItemDto> CreateAsync(CreateTaskItemDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException("el proyecto", dto.ProjectId);
        }

        var task = dto.ToEntity();
        var created = await _taskRepository.AddAsync(task, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return created.ToDto(nowUtc);
    }

    public async Task<TaskItemDto> UpdateAsync(
        int id,
        UpdateTaskItemDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("la tarea", id);

        task.ApplyUpdate(dto);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return task.ToDto(nowUtc);
    }

    public async Task<TaskItemDto> UpdateStatusAsync(
        int id,
        UpdateTaskStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _updateStatusValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("la tarea", id);

        task.ChangeStatus(dto.Status!.Value);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return task.ToDto(nowUtc);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("la tarea", id);

        await _taskRepository.DeleteAsync(task, cancellationToken);
    }
}
