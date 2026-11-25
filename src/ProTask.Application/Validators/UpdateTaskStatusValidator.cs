using FluentValidation;
using ProTask.Application.DTOs.Tasks;

namespace ProTask.Application.Validators;

public sealed class UpdateTaskStatusValidator : AbstractValidator<UpdateTaskStatusDto>
{
    public UpdateTaskStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("El estado es obligatorio.")
            .IsInEnum().WithMessage("El estado especificado no es válido.");
    }
}
