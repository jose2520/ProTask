using FluentValidation;
using ProTask.Application.DTOs.Tasks;

namespace ProTask.Application.Validators;

public sealed class CreateTaskItemValidator : AbstractValidator<CreateTaskItemDto>
{
    public CreateTaskItemValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título de la tarea es obligatorio.")
            .MaximumLength(200).WithMessage("El título de la tarea no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción de la tarea no puede superar los 2000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("La prioridad especificada no es válida.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("La fecha límite es obligatoria.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("El identificador del proyecto debe ser mayor a cero.");
    }
}
