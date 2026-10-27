namespace Reviews.Application.Reviews.Commands;

using MediatR;
using global::Reviews.Domain.Entities;
using global::Reviews.Domain.Interfaces;
using global::Reviews.Domain.ValueObjects;

public record CreateReviewCommand(
    long ProductId,
    string ProductName,
    string AuthorId,
    string AuthorName,
    int Rating,
    string Text) : IRequest<string>;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private readonly IReviewRepository _reviews;

    public CreateReviewCommandHandler(IReviewRepository reviews)
    {
        _reviews = reviews;
    }

    public async Task<string> Handle(CreateReviewCommand cmd, CancellationToken ct)
    {
        var review = new Review(
            cmd.ProductId,
            cmd.ProductName,
            new Author(cmd.AuthorId, cmd.AuthorName),
            cmd.Rating,
            cmd.Text);

        await _reviews.AddAsync(review, ct);
        return review.Id.ToString();
    }
}