using FluentValidation.TestHelper;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Validators;
using ProTask.Domain.Enums;
using Xunit;

namespace ProTask.Application.Tests.Validators;

[Trait("Category", "Unit")]
public sealed class UpdateTaskItemValidatorTests
{
    private readonly UpdateTaskItemValidator _sut = new();

    [Fact]
    public void Validate_WithValidDto_Passes()
    {
        var dto = new UpdateTaskItemDto
        {
            Title = "Tarea actualizada",
            Description = "Descripción válida",
            Status = TaskItemStatus.EnProgreso,
            Priority = TaskPriority.Critica,
            DueDate = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStatusOrPriorityIsNull_Fails()
    {
        var dto = new UpdateTaskItemDto
        {
            Title = "Sin enums",
            Status = null,
            Priority = null,
            DueDate = DateTime.UtcNow.AddDays(2)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void Validate_WithOutOfRangeEnumsAndInvalidTitle_Fails()
    {
        var dto = new UpdateTaskItemDto
        {
            Title = "   ",
            Description = new string('D', 2001),
            Status = (TaskItemStatus)42,
            Priority = (TaskPriority)42,
            DueDate = default
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }
}
