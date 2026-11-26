using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Mappings;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using Xunit;

namespace ProTask.Application.Tests.Mappings;

[Trait("Category", "Unit")]
public sealed class MappingProfileTests
{
    [Fact]
    public void ProjectToDto_MapsProgressAndTaskCounts()
    {
        var project = new Project
        {
            Id = 5,
            Name = "Portal",
            Description = "Desc",
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Tasks =
            [
                new TaskItem { Status = TaskItemStatus.Completada },
                new TaskItem { Status = TaskItemStatus.Pendiente }
            ]
        };

        var dto = project.ToDto();

        Assert.Equal(5, dto.Id);
        Assert.Equal("Portal", dto.Name);
        Assert.Equal(2, dto.TotalTasks);
        Assert.Equal(1, dto.CompletedTasks);
        Assert.Equal(50d, dto.ProgressPercentage);
    }

    [Fact]
    public void CreateProjectDtoToEntity_TrimsTextNormalizesBlankDescriptionAndUtcDates()
    {
        var dto = new CreateProjectDto
        {
            Name = "  Mi Proyecto  ",
            Description = "   ",
            StartDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Unspecified),
            EndDate = new DateTime(2026, 5, 10, 18, 0, 0, DateTimeKind.Unspecified)
        };

        var entity = dto.ToEntity();

        Assert.Equal("Mi Proyecto", entity.Name);
        Assert.Null(entity.Description);
        Assert.Equal(DateTimeKind.Utc, entity.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, entity.EndDate.Kind);
    }

    [Fact]
    public void TaskItemToDto_ComputesIsOverdueAgainstReferenceUtc()
    {
        var referenceUtc = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
        var task = new TaskItem
        {
            Id = 9,
            Title = "Revisar logs",
            Status = TaskItemStatus.EnProgreso,
            Priority = TaskPriority.Alta,
            DueDate = referenceUtc.AddMinutes(-30),
            ProjectId = 2
        };

        var dto = task.ToDto(referenceUtc);

        Assert.Equal(9, dto.Id);
        Assert.Equal(2, dto.ProjectId);
        Assert.True(dto.IsOverdue);
    }

    [Fact]
    public void CreateTaskItemDtoToEntity_SetsDefaultPendienteAndNormalizesUtc()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "  Nueva tarea  ",
            Description = "  Detalle  ",
            Priority = TaskPriority.Critica,
            DueDate = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Unspecified),
            ProjectId = 3
        };

        var entity = dto.ToEntity();

        Assert.Equal("Nueva tarea", entity.Title);
        Assert.Equal("Detalle", entity.Description);
        Assert.Equal(TaskItemStatus.Pendiente, entity.Status);
        Assert.Equal(TaskPriority.Critica, entity.Priority);
        Assert.Equal(DateTimeKind.Utc, entity.DueDate.Kind);
        Assert.Equal(3, entity.ProjectId);
    }

    [Fact]
    public void ApplyUpdate_WhenTaskStatusOrPriorityMissing_ThrowsBusinessRuleException()
    {
        var task = new TaskItem { Title = "Original" };

        Assert.Throws<BusinessRuleException>(() => task.ApplyUpdate(new UpdateTaskItemDto
        {
            Title = "Sin estado",
            Status = null,
            Priority = TaskPriority.Alta,
            DueDate = DateTime.UtcNow
        }));

        Assert.Throws<BusinessRuleException>(() => task.ApplyUpdate(new UpdateTaskItemDto
        {
            Title = "Sin prioridad",
            Status = TaskItemStatus.EnProgreso,
            Priority = null,
            DueDate = DateTime.UtcNow
        }));
    }
}
