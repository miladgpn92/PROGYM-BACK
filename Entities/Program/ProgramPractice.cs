using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class ProgramPractice : SimpleBaseEntity
    {
        public int ProgramId { get; set; }
        public int ProgramRoutineItemId { get; set; }
        public int PracticeId { get; set; }

        // Describes which data fields are relevant
        public PracticeType Type { get; set; }

        // Structured fields (nullable depending on Type)
        public int? SetCount { get; set; }
        public int? MovementCount { get; set; }
        public int? Duration { get; set; }
        public int? Rest { get; set; }
        public int InternalOrder { get; set; }
        public string Notes { get; set; }

        public virtual Program Program { get; set; }
        public virtual ProgramRoutineItem ProgramRoutineItem { get; set; }
        public virtual Practice Practice { get; set; }
    }

    public class ProgramPracticeConfiguration : IEntityTypeConfiguration<ProgramPractice>
    {
        public void Configure(EntityTypeBuilder<ProgramPractice> builder)
        {
            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);
            builder.Property(x => x.InternalOrder)
                   .IsRequired();

            builder.HasOne(x => x.Program)
                   .WithMany(p => p.ProgramPractices)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.ProgramRoutineItem)
                   .WithMany(ri => ri.ProgramPractices)
                   .HasForeignKey(x => x.ProgramRoutineItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Practice)
                   .WithMany(pr => pr.ProgramPractices)
                   .HasForeignKey(x => x.PracticeId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ProgramId, x.ProgramRoutineItemId, x.InternalOrder });
        }
    }
}
