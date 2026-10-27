namespace FitPulse.Dal.Configurations;

using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        // Ідемпотентний статичний сідінг довідників
        builder.HasData(
            new Category { Id = 1, Name = "Спортивне харчування" },
            new Category { Id = 2, Name = "Фітнес-екіпірування" },
            new Category { Id = 3, Name = "Витамини та добавки" }
        );
    }
}