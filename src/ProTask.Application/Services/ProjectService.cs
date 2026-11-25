using FluentValidation;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.Interfaces;
using ProTask.Application.Mappings;
using ProTask.Domain.Exceptions;
using ProTask.Domain.Interfaces;

namespace ProTask.Application.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IValidator<CreateProjectDto> _createValidator;
    private readonly IValidator<UpdateProjectDto> _updateValidator;

    public ProjectService(
        IProjectRepository projectRepository,
        IValidator<CreateProjectDto> createValidator,
        IValidator<UpdateProjectDto> updateValidator)
    {
        _projectRepository = projectRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        return projects.Select(project => project.ToDto()).ToList();
    }

    public async Task<ProjectDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdWithTasksAsync(id, cancellationToken)
            ?? throw new NotFoundException("el proyecto", id);

        return project.ToDto();
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var project = dto.ToEntity();
        var created = await _projectRepository.AddAsync(project, cancellationToken);
        return created.ToDto();
    }

    public async Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var project = await _projectRepository.GetByIdWithTasksAsync(id, cancellationToken)
            ?? throw new NotFoundException("el proyecto", id);

        project.ApplyUpdate(dto);
        await _projectRepository.UpdateAsync(project, cancellationToken);
        return project.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _projectRepository.DeleteIfNoIncompleteTasksAsync(id, cancellationToken);
    }
}
