using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace Entities
{
    public class PracticeCategory : SimpleBaseEntity
    {
        public string Title { get; set; }

        public virtual ICollection<Practice> Practices { get; set; }
    }

    public class PracticeCategoryConfiguration : IEntityTypeConfiguration<PracticeCategory>
    {
        public void Configure(EntityTypeBuilder<PracticeCategory> builder)
        {
            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();
        }
    }
}
