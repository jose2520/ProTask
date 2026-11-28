using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProTask.Application;
using ProTask.Application.DTOs.Projects;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Interfaces;
using ProTask.Domain.Abstractions;
using ProTask.Infrastructure;
using ProTask.Infrastructure.Persistence;
using Xunit;

namespace ProTask.Infrastructure.Tests;

[Trait("Category", "Unit")]
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationAndAddInfrastructure_RegistersAllRequiredServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=protask_test;Username=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        Assert.NotNull(scopedProvider.GetService<AppDbContext>());
        Assert.NotNull(scopedProvider.GetService<HealthCheckService>());
        var healthOptions = scopedProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        Assert.Contains(healthOptions.Registrations, registration => registration.Name == "postgresql");
        Assert.NotNull(scopedProvider.GetService<IProjectRepository>());
        Assert.NotNull(scopedProvider.GetService<ITaskRepository>());
        Assert.NotNull(scopedProvider.GetService<IProjectService>());
        Assert.NotNull(scopedProvider.GetService<ITaskService>());
        Assert.NotNull(scopedProvider.GetService<IValidator<CreateProjectDto>>());
        Assert.NotNull(scopedProvider.GetService<IValidator<UpdateProjectDto>>());
        Assert.NotNull(scopedProvider.GetService<IValidator<CreateTaskItemDto>>());
        Assert.NotNull(scopedProvider.GetService<IValidator<UpdateTaskItemDto>>());
        Assert.NotNull(scopedProvider.GetService<IValidator<UpdateTaskStatusDto>>());
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }
}
