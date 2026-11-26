using FluentValidation.TestHelper;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Validators;
using ProTask.Domain.Enums;
using Xunit;

namespace ProTask.Application.Tests.Validators;

[Trait("Category", "Unit")]
public sealed class CreateTaskItemValidatorTests
{
    private readonly CreateTaskItemValidator _sut = new();

    [Fact]
    public void Validate_WithValidDto_Passes()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "Implementar endpoints",
            Description = "Detalle de prueba",
            Priority = TaskPriority.Alta,
            DueDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            ProjectId = 1
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidFields_FailsOnAllInvalidProperties()
    {
        var dto = new CreateTaskItemDto
        {
            Title = "",
            Description = new string('Z', 2001),
            Priority = (TaskPriority)99,
            DueDate = default,
            ProjectId = 0
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WithTitleOverMaxLength_FailsOnTitle()
    {
        var dto = new CreateTaskItemDto
        {
            Title = new string('T', 201),
            Priority = TaskPriority.Baja,
            DueDate = DateTime.UtcNow.AddDays(1),
            ProjectId = 1
        };

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title);
    }
}
