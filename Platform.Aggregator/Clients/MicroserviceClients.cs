namespace Platform.Aggregator.Clients;

using Platform.Aggregator.Models;

public class FitPulseClient(HttpClient http)
{
    public Task<ProductDto?> GetProductAsync(long id, CancellationToken ct)
        => http.GetFromJsonAsync<ProductDto>($"/api/products/{id}", ct);
}

public class ReviewsClient(HttpClient http)
{
    public async Task<List<ReviewDto>> GetForProductAsync(long productId, CancellationToken ct)
    {
        var result = await http.GetFromJsonAsync<List<ReviewDto>>($"/api/reviews?productId={productId}", ct);
        return result ?? new List<ReviewDto>();
    }
}