namespace Platform.Aggregator.Models;

public record ProductDto(long Id, string Name, decimal Price);
public record ReviewDto(string Id, long ProductId, string AuthorName, int Rating, string Text);

public record FullProductSummaryDto(
    ProductDto? Product,
    IReadOnlyList<ReviewDto> Reviews,
    IReadOnlyList<string> Warnings);