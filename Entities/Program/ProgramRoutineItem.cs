using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace Entities
{
    public class ProgramRoutineItem : SimpleBaseEntity
    {
        public int ProgramId { get; set; }
        public ProgramRoutineItemType ItemType { get; set; }
        public int DisplayOrder { get; set; }
        public string Title { get; set; }
        public int? RepeatCount { get; set; }
        public int? RestBetweenRepeats { get; set; }
        public string Notes { get; set; }

        public virtual Program Program { get; set; }
        public virtual ICollection<ProgramPractice> ProgramPractices { get; set; } = new List<ProgramPractice>();
    }

    public class ProgramRoutineItemConfiguration : IEntityTypeConfiguration<ProgramRoutineItem>
    {
        public void Configure(EntityTypeBuilder<ProgramRoutineItem> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.HasOne(x => x.Program)
                   .WithMany(p => p.ProgramRoutineItems)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ProgramId, x.DisplayOrder });
        }
    }
}

