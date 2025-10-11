using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace Entities
{
    public class Practice : SimpleBaseEntity
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int PracticeCategoryId { get; set; }
        public int? ThumbFileId { get; set; }
        public int? VideoFileId { get; set; }
        public int UserId { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual PracticeCategory PracticeCategory { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual GymFile ThumbFile { get; set; }
        public virtual GymFile VideoFile { get; set; }

        public virtual ICollection<ProgramPractice> ProgramPractices { get; set; }
    }

    public class PracticeConfiguration : IEntityTypeConfiguration<Practice>
    {
        public void Configure(EntityTypeBuilder<Practice> builder)
        {
            builder.Property(x => x.Name)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.Desc)
                   .HasMaxLength(2000);

            builder.HasOne(x => x.PracticeCategory)
                   .WithMany(c => c.Practices)
                   .HasForeignKey(x => x.PracticeCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                   .WithMany(u => u.Practices)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ThumbFile)
                   .WithMany()
                   .HasForeignKey(x => x.ThumbFileId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.VideoFile)
                   .WithMany()
                   .HasForeignKey(x => x.VideoFileId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.Property(x => x.CreateDate);

            builder.HasIndex(x => x.PracticeCategoryId);
            builder.HasIndex(x => x.UserId);
        }
    }
}
