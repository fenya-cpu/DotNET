namespace Reviews.Application.Common.Interfaces;

using global::Reviews.Application.Common.Models;
using global::Reviews.Application.Reviews.Queries;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PagedResult<ReviewDto>> GetListAsync(long? productId, int page, int pageSize, CancellationToken ct = default);
}