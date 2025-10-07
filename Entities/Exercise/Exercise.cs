using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Entities
{
    public class Exercise : SimpleBaseEntity
    {
        public int UserId { get; set; }
        public int ProgramId { get; set; }
        public DateTime Date { get; set; }
        public bool Complete { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual Program Program { get; set; }
    }

    public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
    {
        public void Configure(EntityTypeBuilder<Exercise> builder)
        {
            builder.Property(x => x.Date).IsRequired();

            builder.HasOne(x => x.User)
                   .WithMany(u => u.Exercises)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Program)
                   .WithMany(p => p.Exercises)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.ProgramId, x.Date });
        }
    }
}

