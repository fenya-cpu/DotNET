namespace FitPulse.Bll.DTOs;

public record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int AvailableStock { get; init; }
    public string? Description { get; init; }
    public List<string> Categories { get; init; } = new();
}

public record CreateProductDto(
    string Name,
    string Sku,
    decimal Price,
    int InitialStock = 0,
    string? Description = null,
    List<int>? CategoryIds = null);

public record UpdateStockDto(int Quantity);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}