using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using Common.Enums;

namespace Entities
{
    // Note: Class name 'Program' is per your request.
    // It lives in the Entities namespace and will map to 'Programs' table.
    public class Program : SimpleBaseEntity
    {
        public string Title { get; set; }
        public int CountOfPractice { get; set; }
        public ProgramTypes Type { get; set; }
        public int GymId { get; set; }
        public string Note { get; set; }

        public int? OwnerId { get; set; }
        public int SubmitterUserId { get; set; }
        public System.DateTime CreateDate { get; set; }

        public virtual Gym Gym { get; set; }
        public virtual ApplicationUser Owner { get; set; }
        public virtual ApplicationUser SubmitterUser { get; set; }

        public virtual ICollection<ProgramPractice> ProgramPractices { get; set; }
        public virtual ICollection<ProgramRoutineItem> ProgramRoutineItems { get; set; }
        public virtual ICollection<UserProgram> UserPrograms { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; }
        public virtual ICollection<ProgramPaperFile> PaperFiles { get; set; } = new List<ProgramPaperFile>();
        public virtual ICollection<ProgramCategoryProgram> ProgramCategoryPrograms { get; set; } = new List<ProgramCategoryProgram>();
    }

    public class ProgramConfiguration : IEntityTypeConfiguration<Program>
    {
        public void Configure(EntityTypeBuilder<Program> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            // Enum stored as int by default
            builder.Property(x => x.GymId)
                   .IsRequired();

            builder.HasOne(x => x.Gym)
                   .WithMany(g => g.Programs)
                   .HasForeignKey(x => x.GymId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Owner)
                   .WithMany(u => u.OwnedPrograms)
                   .HasForeignKey(x => x.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SubmitterUser)
                   .WithMany(u => u.SubmittedPrograms)
                   .HasForeignKey(x => x.SubmitterUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.GymId);
            builder.HasIndex(x => x.OwnerId);
            builder.HasIndex(x => x.SubmitterUserId);
            builder.Property(x => x.CreateDate);
            builder.Property(x => x.Note)
                   .HasMaxLength(1000);
            builder.HasMany(x => x.PaperFiles)
                   .WithOne(pf => pf.Program)
                   .HasForeignKey(pf => pf.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ProgramCategoryPrograms)
                   .WithOne(pcp => pcp.Program)
                   .HasForeignKey(pcp => pcp.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
