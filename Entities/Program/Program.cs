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

        public int? OwnerId { get; set; }
        public int SubmitterUserId { get; set; }
        public System.DateTime CreateDate { get; set; }

        public virtual ApplicationUser Owner { get; set; }
        public virtual ApplicationUser SubmitterUser { get; set; }

        public virtual ICollection<ProgramPractice> ProgramPractices { get; set; }
        public virtual ICollection<UserProgram> UserPrograms { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; }
    }

    public class ProgramConfiguration : IEntityTypeConfiguration<Program>
    {
        public void Configure(EntityTypeBuilder<Program> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            // Enum stored as int by default

            builder.HasOne(x => x.Owner)
                   .WithMany(u => u.OwnedPrograms)
                   .HasForeignKey(x => x.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SubmitterUser)
                   .WithMany(u => u.SubmittedPrograms)
                   .HasForeignKey(x => x.SubmitterUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.OwnerId);
            builder.HasIndex(x => x.SubmitterUserId);
            builder.Property(x => x.CreateDate);
        }
    }
}
