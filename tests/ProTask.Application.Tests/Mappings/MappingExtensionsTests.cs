using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Mappings;
using ProTask.Domain.Entities;
using ProTask.Domain.Enums;
using ProTask.Domain.Exceptions;
using Xunit;

namespace ProTask.Application.Tests.Mappings;

[Trait("Category", "Unit")]
public sealed class MappingExtensionsTests
{
    [Fact]
    public void ProjectToDto_MapsProgressAndTaskCounts()
    {
        var project = Project.Create(
            "Portal",
            "Desc",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            id: 5);

        project.AddTasks([
            TaskItem.Create("T1", null, TaskPriority.Media, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), 5, TaskItemStatus.Completada),
            TaskItem.Create("T2", null, TaskPriority.Media, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), 5, TaskItemStatus.Pendiente)
        ]);

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
        var task = TaskItem.Create(
            "Revisar logs",
            null,
            TaskPriority.Alta,
            referenceUtc.AddMinutes(-30),
            projectId: 2,
            status: TaskItemStatus.EnProgreso,
            id: 9);

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
        var task = TaskItem.Create("Original", null, TaskPriority.Media, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));

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
