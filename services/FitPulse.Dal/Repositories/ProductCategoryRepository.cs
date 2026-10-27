namespace FitPulse.Dal.Repositories;

using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(
        string categoryName, DateTime since, CancellationToken ct = default);
}

public class ProductCategoryRepository : GenericRepository<ProductCategory>, IProductCategoryRepository
{
    public ProductCategoryRepository(AppDbContext context) : base(context) { }

    // Вибірка M:N через проміжну сутність та її власність AddedAt
    public async Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(
        string categoryName, DateTime since, CancellationToken ct = default)
    {
        return await Set
            .Where(pc => pc.Category.Name == categoryName && pc.AddedAt >= since)
            .OrderByDescending(pc => pc.AddedAt)
            .Select(pc => pc.Product)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}