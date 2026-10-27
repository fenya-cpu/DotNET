namespace FitPulse.Domain.Entities;

public class ProductDetail
{
    public int ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }

    public Product Product { get; set; } = null!;
}