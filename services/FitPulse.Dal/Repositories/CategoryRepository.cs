namespace FitPulse.Dal.Repositories;

using FitPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default);
}

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    // Explicit Loading: окремий другий запит за вимогою
    public async Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct); // 1-й запит
        if (category is null) return null;

        await Context.Entry(category)
            .Collection(c => c.ProductCategories)
            .Query()
            .Include(pc => pc.Product)
            .LoadAsync(ct); // 2-й запит

        return category;
    }
}