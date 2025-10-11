using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities
{
    public class Gym : BaseWithSeoEntity
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public string LogoUrl { get; set; }

        public string Slug { get; set; }

        // Contact details
        public string ContactUsPhoneNumber { get; set; }

        public string Phone { get; set; }

        // Location
        public double Lat { get; set; }

        public double Lng { get; set; }

        // Social links
        public string InstagramLink { get; set; }

        public string TelegramLink { get; set; }

        public string EitaaLink { get; set; }

        public string BaleLink { get; set; }

        public virtual ICollection<GymUser> GymUsers { get; set; }
    }

    public class GymConfiguration : IEntityTypeConfiguration<Gym>
    {
        public void Configure(EntityTypeBuilder<Gym> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.Address)
                   .HasMaxLength(500);

            builder.Property(x => x.LogoUrl)
                   .HasMaxLength(500);

            builder.Property(x => x.Slug)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.HasIndex(x => x.Slug)
                   .IsUnique();

            builder.Property(x => x.ContactUsPhoneNumber)
                   .HasMaxLength(50);

            builder.Property(x => x.Phone)
                   .HasMaxLength(50);

            builder.Property(x => x.InstagramLink)
                   .HasMaxLength(500);

            builder.Property(x => x.TelegramLink)
                   .HasMaxLength(500);

            builder.Property(x => x.EitaaLink)
                   .HasMaxLength(500);

            builder.Property(x => x.BaleLink)
                   .HasMaxLength(500);
        }
    }
}
