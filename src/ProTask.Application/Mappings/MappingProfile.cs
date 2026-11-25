using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;

namespace ProTask.Application.Mappings;

public static class MappingProfile
{
    public static ProjectDto ToDto(this Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            TotalTasks = project.GetTotalTasksCount(),
            CompletedTasks = project.GetCompletedTasksCount(),
            ProgressPercentage = project.CalculateProgressPercentage()
        };
    }

    public static Project ToEntity(this CreateProjectDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = NormalizeOptionalText(dto.Description)
        };

        project.UpdateDates(dto.StartDate, dto.EndDate);
        return project;
    }

    public static void ApplyUpdate(this Project project, UpdateProjectDto dto)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(dto);

        project.Name = dto.Name.Trim();
        project.Description = NormalizeOptionalText(dto.Description);
        project.UpdateDates(dto.StartDate, dto.EndDate);
    }

    public static TaskItemDto ToDto(this TaskItem task, DateTime referenceUtc)
    {
        ArgumentNullException.ThrowIfNull(task);

        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            ProjectId = task.ProjectId,
            IsOverdue = task.IsOverdue(referenceUtc)
        };
    }

    public static TaskItem ToEntity(this CreateTaskItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = NormalizeOptionalText(dto.Description),
            Status = TaskItemStatus.Pendiente,
            Priority = dto.Priority,
            DueDate = Project.NormalizeToUtc(dto.DueDate),
            ProjectId = dto.ProjectId
        };
    }

    public static void ApplyUpdate(this TaskItem task, UpdateTaskItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(dto);

        task.Title = dto.Title.Trim();
        task.Description = NormalizeOptionalText(dto.Description);
        task.ChangeStatus(dto.Status);
        task.Priority = dto.Priority;
        task.DueDate = Project.NormalizeToUtc(dto.DueDate);
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
