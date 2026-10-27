namespace FitPulse.Dal.Configurations;

using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
            t.HasCheckConstraint("CK_Products_Price", "[Price] > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.Name);

        builder.HasOne(p => p.Detail)
            .WithOne(d => d.Product)
            .HasForeignKey<ProductDetail>(d => d.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}