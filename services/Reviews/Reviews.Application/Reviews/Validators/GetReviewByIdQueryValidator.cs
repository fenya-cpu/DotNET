namespace Reviews.Application.Reviews.Validators;

using FluentValidation;
using MongoDB.Bson;
using global::Reviews.Application.Reviews.Queries;

public class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id має бути валідним ObjectId (24 гекс-символи).");
    }
}