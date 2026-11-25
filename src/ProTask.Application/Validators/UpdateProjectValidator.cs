using FluentValidation;
using ProTask.Application.DTOs.Projects;
using ProTask.Domain.Entities;

namespace ProTask.Application.Validators;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proyecto es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del proyecto no puede superar los 150 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La descripción del proyecto no puede superar los 1000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("La fecha de finalización es obligatoria.")
            .Must((dto, endDate) => Project.NormalizeToUtc(endDate) >= Project.NormalizeToUtc(dto.StartDate))
            .WithMessage("La fecha de finalización debe ser igual o posterior a la fecha de inicio.");
    }
}
