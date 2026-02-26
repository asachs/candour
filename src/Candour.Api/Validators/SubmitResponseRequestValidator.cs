namespace Candour.Api.Validators;

using Candour.Shared.Contracts;
using FastEndpoints;
using FluentValidation;

public class SubmitResponseRequestValidator : Validator<SubmitResponseRequest>
{
    public SubmitResponseRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .MaximumLength(500).WithMessage("Token must not exceed 500 characters");

        RuleFor(x => x.Answers)
            .NotEmpty().WithMessage("Answers are required")
            .Must(a => a.Count <= 100).WithMessage("Cannot exceed 100 answers");

        RuleFor(x => x.Answers)
            .Must(a => a.Keys.All(k => Guid.TryParse(k, out _)))
            .When(x => x.Answers.Count > 0)
            .WithMessage("Answer keys must be valid GUIDs");

        RuleFor(x => x.Answers)
            .Must(a => a.Values.All(v => v.Length <= 5000))
            .When(x => x.Answers.Count > 0)
            .WithMessage("Answer values must not exceed 5000 characters");
    }
}
