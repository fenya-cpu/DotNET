namespace FitPulse.Bll.Validators;

using FitPulse.Bll.DTOs;
using FluentValidation;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва товару обов'язкова.")
            .MaximumLength(200);

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU обов'язковий.")
            .Matches("^[A-Z0-9-]+$").WithMessage("SKU повинен містити тільки великі латинські літери, цифри та дефіс.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ціна повинна бути більшою за 0.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Залишок не може бути від'ємним.");
    }
}