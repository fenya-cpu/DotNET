namespace Reviews.Application.Reviews.Queries;

using MediatR;
using global::Reviews.Application.Common.Interfaces;
using global::Reviews.Application.Common.Models;

public record GetReviewsListQuery(long? ProductId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ReviewDto>>;

public class GetReviewsListQueryHandler : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewQueries _queries;

    public GetReviewsListQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery q, CancellationToken ct)
    {
        return _queries.GetListAsync(q.ProductId, q.Page, Math.Clamp(q.PageSize, 1, 100), ct);
    }
}