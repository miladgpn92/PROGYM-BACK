using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Entities
{
    public class UserProgram : SimpleBaseEntity
    {
        public int UserId { get; set; }
        public int ProgramId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual Program Program { get; set; }
    }

    public class UserProgramConfiguration : IEntityTypeConfiguration<UserProgram>
    {
        public void Configure(EntityTypeBuilder<UserProgram> builder)
        {
            builder.HasOne(x => x.User)
                   .WithMany(u => u.UserPrograms)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Program)
                   .WithMany(p => p.UserPrograms)
                   .HasForeignKey(x => x.ProgramId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.ProgramId });
        }
    }
}

