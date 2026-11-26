using FluentValidation.TestHelper;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.Validators;
using Xunit;

namespace ProTask.Application.Tests.Validators;

[Trait("Category", "Unit")]
public sealed class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _sut = new();

    [Fact]
    public void Validate_WithValidDto_Passes()
    {
        var dto = new CreateProjectDto
        {
            Name = "Proyecto Válido",
            Description = "Descripción opcional",
            StartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_FailsOnName(string name)
    {
        var dto = new CreateProjectDto
        {
            Name = name,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameAndDescriptionOverMaxLength_FailsOnBoth()
    {
        var dto = new CreateProjectDto
        {
            Name = new string('A', 151),
            Description = new string('B', 1001),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenEndDateBeforeStartDate_FailsOnEndDate()
    {
        var dto = new CreateProjectDto
        {
            Name = "Fechas invertidas",
            StartDate = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 9, 23, 59, 59, DateTimeKind.Utc)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Validate_WithDefaultDates_FailsOnStartDateAndEndDate()
    {
        var dto = new CreateProjectDto
        {
            Name = "Sin fechas",
            StartDate = default,
            EndDate = default
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.StartDate);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }
}
