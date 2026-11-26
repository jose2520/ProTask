using FluentValidation.TestHelper;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Validators;
using ProTask.Domain.Enums;
using Xunit;

namespace ProTask.Application.Tests.Validators;

[Trait("Category", "Unit")]
public sealed class UpdateTaskStatusValidatorTests
{
    private readonly UpdateTaskStatusValidator _sut = new();

    [Theory]
    [InlineData(TaskItemStatus.Pendiente)]
    [InlineData(TaskItemStatus.EnProgreso)]
    [InlineData(TaskItemStatus.Completada)]
    public void Validate_WithDefinedStatus_Passes(TaskItemStatus status)
    {
        var result = _sut.TestValidate(new UpdateTaskStatusDto { Status = status });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStatusIsNull_FailsOnStatus()
    {
        var result = _sut.TestValidate(new UpdateTaskStatusDto { Status = null });

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_WhenStatusIsUndefinedEnum_FailsOnStatus()
    {
        var result = _sut.TestValidate(new UpdateTaskStatusDto { Status = (TaskItemStatus)777 });

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
