using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProTask.Application.Interfaces;
using ProTask.Application.Services;
using ProTask.Application.Validators;

namespace ProTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();

        return services;
    }
}
