namespace FitPulse.Dal.Specifications;

using Ardalis.Specification;
using FitPulse.Domain.Entities;

public record ProductQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string SortBy = "name",
    string SortDir = "asc");

public class ProductsFilterSpec : Specification<Product>
{
    public ProductsFilterSpec(ProductQuery q, int pageSize)
    {
        Query.Include(p => p.ProductCategories)
             .ThenInclude(pc => pc.Category);

        if (!string.IsNullOrWhiteSpace(q.Search))
            Query.Where(p => p.Name.Contains(q.Search));

        if (q.MinPrice.HasValue)
            Query.Where(p => p.Price >= q.MinPrice.Value);

        if (q.MaxPrice.HasValue)
            Query.Where(p => p.Price <= q.MaxPrice.Value);

        if (q.SortBy.ToLower() == "price")
        {
            if (q.SortDir.ToLower() == "desc") Query.OrderByDescending(p => p.Price);
            else Query.OrderBy(p => p.Price);
        }
        else
        {
            if (q.SortDir.ToLower() == "desc") Query.OrderByDescending(p => p.Name);
            else Query.OrderBy(p => p.Name);
        }

        Query.Skip((q.Page - 1) * pageSize).Take(pageSize);
    }
}