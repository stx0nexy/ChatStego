using FluentValidation;
using PixChat.Application.DTOs;

namespace PixChat.Application.Validators;

public class CreateChatDtoValidator : AbstractValidator<CreateChatDto>
{
    public CreateChatDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Chat name is required.")
            .Length(3, 255).WithMessage("Chat name must be between 3 and 255 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.CreatorId)
            .GreaterThan(0).WithMessage("Creator ID must be a positive integer.");

        RuleFor(x => x.ParticipantIds)
            .NotNull().WithMessage("Participants list cannot be null.")
            .Must(ids => ids.Count >= 1).WithMessage("At least one participant is required besides the creator.");
    }
}