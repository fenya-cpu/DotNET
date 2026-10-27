namespace FitPulse.Bll.Services;

using AutoMapper;
using FitPulse.Bll.DTOs;

using FitPulse.Dal;
using FitPulse.Dal.Specifications;
using FitPulse.Domain.Entities;
using FitPulse.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> GetListAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default);
    Task AddStockAsync(int id, int quantity, CancellationToken ct = default);
    Task RemoveStockAsync(int id, int quantity, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context; // Для LINQ точок специфікацій/пагінації

    public ProductService(IUnitOfWork uow, IMapper mapper, AppDbContext context)
    {
        _uow = uow;
        _mapper = mapper;
        _context = context;
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithDetailsAsync(id, ct);
        if (product is null)
            throw new EntityNotFoundException(nameof(Product), id);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<PagedResult<ProductDto>> GetListAsync(ProductQuery q, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(p => p.Name.Contains(q.Search));

        if (q.MinPrice.HasValue) query = query.Where(p => p.Price >= q.MinPrice.Value);
        if (q.MaxPrice.HasValue) query = query.Where(p => p.Price <= q.MaxPrice.Value);

        query = (q.SortBy.ToLower(), q.SortDir.ToLower()) switch
        {
            ("price", "desc") => query.OrderByDescending(p => p.Price),
            ("price", _) => query.OrderBy(p => p.Price),
            (_, "desc") => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(p => p.Detail)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Skip((q.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(
            _mapper.Map<List<ProductDto>>(items), total, q.Page, pageSize);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default)
    {
        if (await _uow.Products.ExistsBySkuAsync(input.Sku, ct))
            throw new ConflictException($"Товар з SKU '{input.Sku}' вже існує.");

        var product = new Product(input.Name, input.Sku, input.Price, input.InitialStock);

        await _uow.Products.AddAsync(product, ct);

        if (input.CategoryIds != null && input.CategoryIds.Any())
        {
            foreach (var catId in input.CategoryIds)
            {
                await _uow.ProductCategories.AddAsync(new ProductCategory
                {
                    Product = product,
                    CategoryId = catId,
                    AddedAt = DateTime.UtcNow
                }, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task AddStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product is null) throw new EntityNotFoundException(nameof(Product), id);

        product.AddStock(quantity);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task RemoveStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product is null) throw new EntityNotFoundException(nameof(Product), id);

        product.RemoveStock(quantity);
        await _uow.SaveChangesAsync(ct);
    }
}