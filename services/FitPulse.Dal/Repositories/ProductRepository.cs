namespace FitPulse.Dal.Repositories;

using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
}

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    // Eager Loading: один SQL запит з JOIN
    public async Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return await Set
            .Include(p => p.Detail)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await Set.AnyAsync(p => p.Sku == sku, ct);
    }
}