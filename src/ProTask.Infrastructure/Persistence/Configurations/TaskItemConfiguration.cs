using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProTask.Domain.Entities;

namespace ProTask.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .ValueGeneratedOnAdd();

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(task => task.Description)
            .HasMaxLength(2000);

        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(task => task.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(task => task.DueDate)
            .IsRequired();

        builder.Property(task => task.ProjectId)
            .IsRequired();

        builder.HasIndex(task => task.ProjectId)
            .HasDatabaseName("ix_tasks_project_id");

        builder.HasIndex(task => new { task.Status, task.Priority })
            .HasDatabaseName("ix_tasks_status_priority");

        builder.HasIndex(task => new { task.Status, task.DueDate })
            .HasDatabaseName("ix_tasks_status_due_date");

        builder.HasIndex(task => task.Priority)
            .HasDatabaseName("ix_tasks_priority");
    }
}
