namespace Reviews.Application.Reviews.Validators;

using FluentValidation;
using global::Reviews.Application.Reviews.Commands;
using global::Reviews.Domain.Interfaces;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) => !await reviews.ExistsAsync(cmd.ProductId, cmd.AuthorId, ct))
            .WithMessage("Ви вже залишали відгук на цей товар.");
    }
}