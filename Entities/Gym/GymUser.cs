using Common.Consts;
using Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class GymUser : IEntity
    {
        public int GymId { get; set; }
        public int UserId { get; set; }
        public UsersRole Role { get; set; }

        public virtual Gym Gym { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

    public class GymUserConfiguration : IEntityTypeConfiguration<GymUser>
    {
        public void Configure(EntityTypeBuilder<GymUser> builder)
        {
            builder.HasKey(x => new { x.GymId, x.UserId });

            builder.Property(x => x.Role)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasOne(x => x.Gym)
                   .WithMany(g => g.GymUsers)
                   .HasForeignKey(x => x.GymId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                   .WithMany(u => u.GymUsers)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

