using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class ProgramPaperFile : SimpleBaseEntity
    {
        public int ProgramId { get; set; }
        public int GymFileId { get; set; }
        public int DisplayOrder { get; set; }

        public virtual Program Program { get; set; }
        public virtual GymFile GymFile { get; set; }
    }

    public class ProgramPaperFileConfiguration : IEntityTypeConfiguration<ProgramPaperFile>
    {
        public void Configure(EntityTypeBuilder<ProgramPaperFile> builder)
        {
            builder.Property(x => x.DisplayOrder)
                   .IsRequired();

            builder.HasOne(x => x.Program)
                   .WithMany(p => p.PaperFiles)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.GymFile)
                   .WithMany()
                   .HasForeignKey(x => x.GymFileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ProgramId, x.GymFileId })
                   .IsUnique();
        }
    }
}

