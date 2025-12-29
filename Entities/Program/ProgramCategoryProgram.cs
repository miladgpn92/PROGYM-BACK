using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class ProgramCategoryProgram : SimpleBaseEntity
    {
        public int ProgramId { get; set; }
        public int ProgramCategoryId { get; set; }

        public virtual Program Program { get; set; }
        public virtual ProgramCategory ProgramCategory { get; set; }
    }

    public class ProgramCategoryProgramConfiguration : IEntityTypeConfiguration<ProgramCategoryProgram>
    {
        public void Configure(EntityTypeBuilder<ProgramCategoryProgram> builder)
        {
            builder.HasOne(x => x.Program)
                   .WithMany(p => p.ProgramCategoryPrograms)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ProgramCategory)
                   .WithMany(pc => pc.ProgramCategoryPrograms)
                   .HasForeignKey(x => x.ProgramCategoryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ProgramId, x.ProgramCategoryId });
        }
    }
}
