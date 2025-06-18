using FluentValidation;
using PixChat.Application.Requests;

namespace PixChat.Application.Validators;

public class Verify2FARequestValidator : AbstractValidator<Verify2FARequest>
{
    public Verify2FARequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits long.")
            .Matches("^[0-9]+$").WithMessage("Verification code must contain only digits.");
    }
}