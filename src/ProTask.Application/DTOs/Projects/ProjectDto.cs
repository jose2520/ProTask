namespace ProTask.Application.DTOs.Projects;

public sealed record ProjectDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public double ProgressPercentage { get; init; }
}
