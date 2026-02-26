namespace Candour.Application.Surveys;

using Candour.Core.Entities;
using Candour.Core.Enums;
using Candour.Core.Interfaces;
using MediatR;

public record CreateSurveyCommand(
    string Title,
    string Description,
    string CreatorId,
    int AnonymityThreshold,
    int TimestampJitterMinutes,
    List<CreateQuestionItem> Questions
) : IRequest<Survey>;

public record CreateQuestionItem(
    QuestionType Type,
    string Text,
    List<string> Options,
    bool Required,
    int Order
);

public class CreateSurveyHandler : IRequestHandler<CreateSurveyCommand, Survey>
{
    private readonly ISurveyRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly IBatchSecretProtector _protector;

    public CreateSurveyHandler(ISurveyRepository repo, ITokenService tokenService, IBatchSecretProtector protector)
    {
        _repo = repo;
        _tokenService = tokenService;
        _protector = protector;
    }

    public async Task<Survey> Handle(CreateSurveyCommand request, CancellationToken ct)
    {
        var survey = new Survey
        {
            Title = request.Title,
            Description = request.Description,
            CreatorId = request.CreatorId,
            AnonymityThreshold = request.AnonymityThreshold > 0 ? request.AnonymityThreshold : 5,
            TimestampJitterMinutes = request.TimestampJitterMinutes >= 0 ? request.TimestampJitterMinutes : 10,
            BatchSecret = _protector.Protect(_tokenService.GenerateBatchSecret()),
            Questions = request.Questions.Select(q => new Question
            {
                Type = q.Type,
                Text = q.Text,
                Options = System.Text.Json.JsonSerializer.Serialize(q.Options),
                Required = q.Required,
                Order = q.Order
            }).ToList()
        };

        return await _repo.AddAsync(survey, ct);
    }
}
