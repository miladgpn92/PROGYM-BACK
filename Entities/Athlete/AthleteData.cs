using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Entities
{
    public class AthleteData : SimpleBaseEntity
    {
        public int UserId { get; set; }
        public DateTime SubmitDate { get; set; }
        public int Height { get; set; }
        public int Age { get; set; }
        public decimal Weight { get; set; }

        public virtual ApplicationUser User { get; set; }
    }

    public class AthleteDataConfiguration : IEntityTypeConfiguration<AthleteData>
    {
        public void Configure(EntityTypeBuilder<AthleteData> builder)
        {
            builder.Property(x => x.SubmitDate)
                   .IsRequired();

            builder.Property(x => x.Weight)
                   .HasColumnType("decimal(6,2)");

            builder.HasOne(x => x.User)
                   .WithMany(u => u.AthleteDatas)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.SubmitDate });
        }
    }
}

