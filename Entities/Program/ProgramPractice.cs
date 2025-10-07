using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class ProgramPractice : SimpleBaseEntity
    {
        public int ProgramId { get; set; }
        public int PracticeId { get; set; }

        public PracticeType Type { get; set; }
        public string PracticeData { get; set; }

        public virtual Program Program { get; set; }
        public virtual Practice Practice { get; set; }
    }

    public class ProgramPracticeConfiguration : IEntityTypeConfiguration<ProgramPractice>
    {
        public void Configure(EntityTypeBuilder<ProgramPractice> builder)
        {
            builder.Property(x => x.PracticeData)
                   .HasMaxLength(2000);

            builder.HasOne(x => x.Program)
                   .WithMany(p => p.ProgramPractices)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Practice)
                   .WithMany(pr => pr.ProgramPractices)
                   .HasForeignKey(x => x.PracticeId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ProgramId, x.PracticeId });
        }
    }
}

