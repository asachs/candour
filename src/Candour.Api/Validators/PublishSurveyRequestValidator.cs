namespace Candour.Api.Validators;

using Candour.Api.Endpoints.Survey;
using FastEndpoints;
using FluentValidation;

public class PublishSurveyRequestValidator : Validator<PublishSurveyRequest>
{
    public PublishSurveyRequestValidator()
    {
        RuleFor(x => x.TokenCount)
            .InclusiveBetween(1, 10_000).WithMessage("TokenCount must be between 1 and 10,000");
    }
}
