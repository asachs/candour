namespace Candour.Application.Tests;

using Moq;
using Candour.Core.Entities;
using Candour.Core.Interfaces;
using Candour.Application.Surveys;

public class ListSurveysHandlerTests
{
    private readonly Mock<ISurveyRepository> _repo = new();
    private readonly ListSurveysHandler _handler;

    public ListSurveysHandlerTests()
    {
        _handler = new ListSurveysHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WithCreatorId_CallsGetByCreatorAsync()
    {
        // Arrange
        var creatorId = "creator-42";
        var surveys = new List<Survey>
        {
            new() { Title = "Survey A", CreatorId = creatorId },
            new() { Title = "Survey B", CreatorId = creatorId }
        };
        _repo.Setup(r => r.GetByCreatorAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(surveys);

        // Act
        var result = await _handler.Handle(new ListSurveysQuery(creatorId), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        _repo.Verify(r => r.GetByCreatorAsync(creatorId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullCreatorId_CallsListAsync()
    {
        // Arrange
        var surveys = new List<Survey>
        {
            new() { Title = "Survey X" },
            new() { Title = "Survey Y" },
            new() { Title = "Survey Z" }
        };
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(surveys);

        // Act
        var result = await _handler.Handle(new ListSurveysQuery(null), CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        _repo.Verify(r => r.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GetByCreatorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyCreatorId_CallsListAsync()
    {
        // Arrange
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey>());

        // Act
        var result = await _handler.Handle(new ListSurveysQuery(""), CancellationToken.None);

        // Assert
        Assert.Empty(result);
        _repo.Verify(r => r.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GetByCreatorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DefaultQuery_CallsListAsync()
    {
        // Arrange - default constructor has CreatorId = null
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey>());

        // Act
        var result = await _handler.Handle(new ListSurveysQuery(), CancellationToken.None);

        // Assert
        _repo.Verify(r => r.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
