namespace FitPulse.Api.Data;

using FitPulse.Dal;
using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return; // Уже наповнено

        var whey = new Product("Whey Protein Isolate 1kg", "WHEY-ISO-1KG", 1299.00m, 50);
        var creatine = new Product("Creatine Monohydrate 500g", "CREATINE-500G", 599.00m, 100);

        db.Products.AddRange(whey, creatine);

        db.ProductCategories.Add(new ProductCategory { Product = whey, CategoryId = 1 });
        db.ProductCategories.Add(new ProductCategory { Product = creatine, CategoryId = 1 });

        await db.SaveChangesAsync(ct);
    }
}