using ClassroomService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassroomService.Infrastructure.Persistence.Configurations;

public class TopicWeightHistoryConfiguration : IEntityTypeConfiguration<TopicWeightHistory>
{
    public void Configure(EntityTypeBuilder<TopicWeightHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.TopicWeightId)
            .IsRequired();

        builder.Property(h => h.TopicId)
            .IsRequired();

        builder.Property(h => h.TermName)
            .HasMaxLength(100);

        builder.Property(h => h.OldWeightPercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(h => h.NewWeightPercentage)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(h => h.ModifiedBy)
            .IsRequired();

        builder.Property(h => h.ModifiedAt)
            .IsRequired();

        builder.Property(h => h.Action)
            .IsRequired();

        builder.Property(h => h.ChangeReason)
            .HasMaxLength(500);

        builder.Property(h => h.AffectedTerms)
            .HasMaxLength(200);

        // Relationships
        builder.HasOne(h => h.TopicWeight)
            .WithMany()
            .HasForeignKey(h => h.TopicWeightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Topic)
            .WithMany()
            .HasForeignKey(h => h.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.CourseCode)
            .WithMany()
            .HasForeignKey(h => h.CourseCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.SpecificCourse)
            .WithMany()
            .HasForeignKey(h => h.SpecificCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Term)
            .WithMany()
            .HasForeignKey(h => h.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(h => h.TopicWeightId);
        builder.HasIndex(h => h.ModifiedAt);
        builder.HasIndex(h => h.Action);
        builder.HasIndex(h => h.TermId);
    }
}
