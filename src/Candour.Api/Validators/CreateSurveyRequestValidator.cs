namespace Candour.Api.Validators;

using Candour.Core.Enums;
using Candour.Shared.Contracts;
using FastEndpoints;
using FluentValidation;

public class CreateSurveyRequestValidator : Validator<CreateSurveyRequest>
{
    public CreateSurveyRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.AnonymityThreshold)
            .InclusiveBetween(1, 1000).WithMessage("AnonymityThreshold must be between 1 and 1000");

        RuleFor(x => x.TimestampJitterMinutes)
            .InclusiveBetween(0, 1440).WithMessage("TimestampJitterMinutes must be between 0 and 1440");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("At least one question is required")
            .Must(q => q.Count <= 100).WithMessage("Cannot exceed 100 questions");

        RuleForEach(x => x.Questions).ChildRules(q =>
        {
            q.RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Question type is required")
                .Must(t => Enum.TryParse<QuestionType>(t, ignoreCase: true, out _))
                .WithMessage("Invalid question type");

            q.RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Question text is required")
                .MaximumLength(1000).WithMessage("Question text must not exceed 1000 characters");

            q.RuleFor(x => x.Options)
                .Must(o => o.Count <= 50).WithMessage("Cannot exceed 50 options per question");

            q.RuleForEach(x => x.Options)
                .MaximumLength(500).WithMessage("Option text must not exceed 500 characters");
        });
    }
}
