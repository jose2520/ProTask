using FluentValidation.TestHelper;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.Validators;
using Xunit;

namespace ProTask.Application.Tests.Validators;

[Trait("Category", "Unit")]
public sealed class UpdateProjectValidatorTests
{
    private readonly UpdateProjectValidator _sut = new();

    [Fact]
    public void Validate_WithValidDto_Passes()
    {
        var dto = new UpdateProjectDto
        {
            Name = "Proyecto Editado",
            Description = null,
            StartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyNameOrTooLongFields_Fails()
    {
        var emptyNameDto = new UpdateProjectDto
        {
            Name = " ",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        var oversizedDto = new UpdateProjectDto
        {
            Name = new string('X', 151),
            Description = new string('Y', 1001),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        _sut.TestValidate(emptyNameDto).ShouldHaveValidationErrorFor(x => x.Name);

        var oversizedResult = _sut.TestValidate(oversizedDto);
        oversizedResult.ShouldHaveValidationErrorFor(x => x.Name);
        oversizedResult.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenEndDateBeforeStartDate_FailsOnEndDate()
    {
        var dto = new UpdateProjectDto
        {
            Name = "Inconsistente",
            StartDate = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }
}
