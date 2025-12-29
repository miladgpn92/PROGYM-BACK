using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace Entities
{
    public class ProgramCategory : SimpleBaseEntity
    {
        public string Title { get; set; }
        public int SubmitterUserId { get; set; }
        public int GymId { get; set; }

        public virtual Gym Gym { get; set; }
        public virtual ApplicationUser SubmitterUser { get; set; }
        public virtual ICollection<ProgramCategoryProgram> ProgramCategoryPrograms { get; set; } = new List<ProgramCategoryProgram>();
    }

    public class ProgramCategoryConfiguration : IEntityTypeConfiguration<ProgramCategory>
    {
        public void Configure(EntityTypeBuilder<ProgramCategory> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.HasOne(x => x.SubmitterUser)
                   .WithMany()
                   .HasForeignKey(x => x.SubmitterUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Gym)
                   .WithMany(g => g.ProgramCategories)
                   .HasForeignKey(x => x.GymId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SubmitterUserId);
            builder.HasIndex(x => x.GymId);
        }
    }
}
