namespace Platform.Aggregator.Dtos;

public class ProductFullDetailsDto
{
    public ProductDto? Product { get; set; }
    public List<ReviewDto> Reviews { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
}

public class ReviewDto
{
    public string Id { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
}
