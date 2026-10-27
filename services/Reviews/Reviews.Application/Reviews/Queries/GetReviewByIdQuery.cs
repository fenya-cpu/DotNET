namespace Reviews.Application.Reviews.Queries;

using MediatR;
using global::Reviews.Application.Common.Interfaces;
using global::Reviews.Domain.Exceptions;

public record GetReviewByIdQuery(string Id) : IRequest<ReviewDto>;

public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    private readonly IReviewQueries _queries;

    public GetReviewByIdQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<ReviewDto> Handle(GetReviewByIdQuery request, CancellationToken ct)
    {
        var review = await _queries.GetByIdAsync(request.Id, ct);
        if (review is null)
            throw new NotFoundException("Review", request.Id);

        return review;
    }
}