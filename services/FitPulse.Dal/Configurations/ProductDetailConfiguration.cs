namespace FitPulse.Dal.Configurations;

using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        builder.ToTable("ProductDetails");
        builder.HasKey(pd => pd.ProductId);
        builder.Property(pd => pd.Description).HasMaxLength(2000);
        builder.Property(pd => pd.Manufacturer).HasMaxLength(100);
    }
}