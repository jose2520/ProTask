using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProTask.Domain.Entities;

namespace ProTask.Infrastructure.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .ValueGeneratedOnAdd();

        builder.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(project => project.Description)
            .HasMaxLength(1000);

        builder.Property(project => project.StartDate)
            .IsRequired();

        builder.Property(project => project.EndDate)
            .IsRequired();

        builder.HasMany(project => project.Tasks)
            .WithOne(task => task.Project)
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
