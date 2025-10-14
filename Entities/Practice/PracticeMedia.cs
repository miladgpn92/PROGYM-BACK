using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class PracticeMedia : SimpleBaseEntity
    {
        public int PracticeId { get; set; }
        public int GymFileId { get; set; }
        public MediaFileType MediaType { get; set; }
        public int DisplayOrder { get; set; }

        public virtual Practice Practice { get; set; }
        public virtual GymFile GymFile { get; set; }
    }

    public class PracticeMediaConfiguration : IEntityTypeConfiguration<PracticeMedia>
    {
        public void Configure(EntityTypeBuilder<PracticeMedia> builder)
        {
            builder.Property(x => x.DisplayOrder)
                   .IsRequired();

            builder.HasOne(x => x.Practice)
                   .WithMany(p => p.MediaItems)
                   .HasForeignKey(x => x.PracticeId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.GymFile)
                   .WithMany()
                   .HasForeignKey(x => x.GymFileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.PracticeId, x.MediaType, x.DisplayOrder });
        }
    }
}
