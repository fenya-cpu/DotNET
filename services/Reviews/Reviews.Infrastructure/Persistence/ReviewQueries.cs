namespace Reviews.Infrastructure.Persistence;

using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Queries;
using Reviews.Domain.Entities;

public class ReviewQueries : IReviewQueries
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewQueries(MongoDbContext context)
    {
        _reviews = context.Reviews;
    }

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out var objectId)) return null;

        var review = await _reviews.Find(r => r.Id == objectId).FirstOrDefaultAsync(ct);
        if (review is null) return null;

        return new ReviewDto(
            review.Id.ToString(),
            review.ProductId,
            review.ProductName,
            review.Author.DisplayName,
            review.Rating,
            review.Text,
            review.Comments.Count,
            review.Version);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(long? productId, int page, int pageSize, CancellationToken ct = default)
    {
        var filter = productId.HasValue
            ? Builders<Review>.Filter.Eq(r => r.ProductId, productId.Value)
            : Builders<Review>.Filter.Empty;

        var total = (int)await _reviews.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _reviews.Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(r => new ReviewDto(
            r.Id.ToString(),
            r.ProductId,
            r.ProductName,
            r.Author.DisplayName,
            r.Rating,
            r.Text,
            r.Comments.Count,
            r.Version)).ToList();

        return new PagedResult<ReviewDto>(dtos, total, page, pageSize);
    }
}