using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class GymFile : BaseEntity
    {
        public int GymId { get; set; }

        public string OriginalFileName { get; set; }

        public string StoredFileName { get; set; }

        public string RelativePath { get; set; }

        public string ContentType { get; set; }

        public long SizeBytes { get; set; }

        public bool IsImage { get; set; }

        public int UploadedByUserId { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime UploadedAt { get; set; }

        public virtual Gym Gym { get; set; }

        public virtual ApplicationUser UploadedByUser { get; set; }
    }

    public class GymFileConfiguration : IEntityTypeConfiguration<GymFile>
    {
        public void Configure(EntityTypeBuilder<GymFile> builder)
        {
            builder.Property(x => x.OriginalFileName)
                   .HasMaxLength(260)
                   .IsRequired();

            builder.Property(x => x.StoredFileName)
                   .HasMaxLength(260)
                   .IsRequired();

            builder.Property(x => x.RelativePath)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(x => x.ContentType)
                   .HasMaxLength(150);

            builder.Property(x => x.SizeBytes)
                   .IsRequired();

            builder.Property(x => x.UploadedAt)
                   .IsRequired();

            builder.HasOne(x => x.UploadedByUser)
                   .WithMany()
                   .HasForeignKey(x => x.UploadedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.GymId, x.StoredFileName })
                   .IsUnique();
        }
    }
}
