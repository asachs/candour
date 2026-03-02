# Testing

Candour employs a three-tier testing strategy: unit tests, integration tests, and end-to-end browser tests. Each tier catches a different class of defect.

| Tier | What It Tests | Runs In | Framework |
|------|--------------|---------|-----------|
| **Unit** | Individual handlers, entities, middleware logic, crypto, document mapping, anonymity contracts | CI on every push/PR | xUnit + Moq |
| **Integration** | In-process API with mocked Cosmos DB (handler + middleware pipeline) | CI on every push/PR | xUnit + TestHost |
| **E2E** | Full browser automation against live deployment | Post-deploy in release pipeline | Playwright (TypeScript) |

## Running Tests

### All Tests (Unit + Integration)

```bash
# From the repository root
dotnet test
```

This discovers and runs every `.Tests` project in the solution. All unit and integration tests run against mocked dependencies and require no external services.

### A Specific Test Project

```bash
dotnet test tests/Candour.Application.Tests
```

### With Detailed Output

```bash
dotnet test --verbosity normal
```

### E2E Tests (Playwright)

E2E tests require a running deployment. They are configured for CI but can also be run locally against a local dev instance:

```bash
cd tests/e2e
npm install
npx playwright install chromium
npx playwright test
```

!!! note "E2E Tests Consume Tokens"
    Each E2E test run that exercises the respondent flow consumes real tokens from a test survey. See the [Integration Testing Design](../design/integration-testing.md) document for the token refresh strategy.

## Unit Test Projects

### Candour.Core.Tests

Tests domain entities, enums, and value objects.

| Test File | What It Covers |
|-----------|---------------|
| `EntityDefaultTests.cs` | Default property values and constructor behavior for all domain entities |
| `EnumTests.cs` | Enum value definitions and serialization |
| `ValueObjectTests.cs` | Value object equality, hashing, and validation |

### Candour.Application.Tests

Tests MediatR command and query handlers. Each handler is tested in isolation with mocked repository interfaces.

| Test File | What It Covers |
|-----------|---------------|
| `CreateSurveyHandlerTests.cs` | Survey creation with questions, validation, default values |
| `GetSurveyHandlerTests.cs` | Survey retrieval, not-found handling |
| `ListSurveysHandlerTests.cs` | Survey listing, empty state |
| `PublishSurveyHandlerTests.cs` | Publishing flow, token generation, status transition |
| `CloseSurveyHandlerTests.cs` | Survey closure, status transition |
| `SubmitResponseHandlerTests.cs` | Response submission, token validation, timestamp jitter |
| `GetAggregateResultsHandlerTests.cs` | Result aggregation, threshold enforcement |
| `RunAiAnalysisHandlerTests.cs` | AI analysis handler |

### Candour.Functions.Tests

Tests the Azure Functions middleware pipeline and authentication logic without starting a real HTTP server.

| Test File | What It Covers |
|-----------|---------------|
| `AuthenticationMiddlewareRouteTests.cs` | Route-based auth enforcement (admin vs. public routes) |
| `AnonymityMiddlewarePatternTests.cs` | IP header stripping regex patterns |
| `AuthHelperTests.cs` | API key and JWT validation logic |
| `JwtTokenValidatorTests.cs` | JWT token parsing and validation |
| `RateLimitingMiddlewareTests.cs` | Rate limit enforcement behavior |

### Candour.Infrastructure.Tests

Tests infrastructure implementations including cryptographic services and repository logic.

| Test File | What It Covers |
|-----------|---------------|
| `BlindTokenServiceTests.cs` | HMAC-SHA256 token generation and validation |
| `TimestampJitterServiceTests.cs` | Jitter range and randomness |
| `SurveyRepositoryTests.cs` | Survey persistence logic |
| `ResponseRepositoryTests.cs` | Response persistence logic |
| `DependencyInjectionTests.cs` | DI registration completeness |
| `NullAnalyzerTests.cs` | No-op AI analyzer behavior |
| `OllamaAnalyzerTests.cs` | Ollama AI integration |

### Candour.Infrastructure.Cosmos.Tests

Tests Cosmos DB document mapping between domain entities and Cosmos documents.

| Test File | What It Covers |
|-----------|---------------|
| `SurveyDocumentTests.cs` | Survey entity to/from Cosmos document mapping |
| `ResponseDocumentTests.cs` | Response entity to/from Cosmos document mapping |
| `UsedTokenDocumentTests.cs` | UsedToken entity to/from Cosmos document mapping |

### Candour.Anonymity.Tests

Cross-cutting anonymity contract tests that verify Candour's privacy guarantees hold across the full stack. These are some of the most important tests in the suite.

| Test File | What It Covers |
|-----------|---------------|
| `NoIpLeakageTests.cs` | Verifies that IP-related headers are stripped before reaching handlers |
| `TokenBlindnessTests.cs` | Verifies that token values cannot be correlated with response records |
| `ResponseUnlinkabilityTests.cs` | Verifies that there is no foreign key between `usedTokens` and `responses` |
| `ThresholdGateTests.cs` | Verifies that results are blocked when response count is below threshold |
| `TimestampJitterTests.cs` | Verifies that stored timestamps differ from actual submission times |

!!! tip "Anonymity Tests as Living Documentation"
    The `Candour.Anonymity.Tests` project serves double duty: it validates privacy guarantees in CI and it documents exactly what those guarantees are. When evaluating whether a change could break anonymity, start by reading these tests.

## Integration Tests

### Candour.Functions.Integration.Tests

Tests the API endpoints in-process with mocked Cosmos DB repositories. These tests exercise the full middleware pipeline (authentication, anonymity stripping, rate limiting) alongside handler logic, catching regressions that unit tests miss.

| Test File | What It Covers |
|-----------|---------------|
| `SurveyEndpointTests.cs` | `GET /api/surveys`, `POST /api/surveys`, survey CRUD through HTTP |
| `ResponseEndpointTests.cs` | `POST /api/surveys/{id}/responses`, token validation through HTTP |
| `AuthMiddlewareTests.cs` | 401 enforcement on admin routes, API key acceptance |
| `AnonymityMiddlewareTests.cs` | Header stripping verification through the full pipeline |
| `AggregateResultsTests.cs` | Results endpoint with threshold enforcement |
| `IntegrationTestFixture.cs` | Shared test host bootstrap (mock repositories, disabled auth) |

```csharp
// Example: Verifying admin auth enforcement
[Fact]
public async Task ListSurveys_WithoutAuth_Returns401()
{
    var response = await _client.GetAsync("/api/surveys");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

## End-to-End Tests (Playwright)

Located in `tests/e2e/`, these TypeScript tests run a headless Chromium browser against a live Candour deployment.

| Test File | What It Covers |
|-----------|---------------|
| `anonymous-survey.spec.ts` | Full respondent flow: consent gate, form fill, anonymous submission |
| `admin-dashboard.spec.ts` | Admin login via Entra ID, dashboard rendering, survey list |
| `auth-enforcement.spec.ts` | Redirect to login for unauthenticated admin access |
| `404-page.spec.ts` | Styled 404 page for unknown routes |

!!! warning "E2E Tests Run Against Live Infrastructure"
    Playwright tests hit real Azure resources. They can be flaky during Azure outages or cold starts. Set generous timeouts (Blazor WASM initial load can take 5--15 seconds) and use retry policies in CI.

## Adding New Tests

### Adding a Handler Test

1. Identify the handler in `src/Candour.Application/`.
2. Create a test class in `tests/Candour.Application.Tests/` following the naming convention `{HandlerName}Tests.cs`.
3. Use Moq to mock the repository interfaces from `Candour.Core`.
4. Send a command/query through MediatR and assert the result.

```csharp
public class CreateSurveyHandlerTests
{
    private readonly Mock<ISurveyRepository> _surveyRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly IMediator _mediator;

    public CreateSurveyHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(_surveyRepo.Object);
        services.AddSingleton(_tokenService.Object);
        _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task CreateSurvey_WithValidInput_ReturnsSurveyDto()
    {
        // Arrange
        _surveyRepo.Setup(r => r.CreateAsync(It.IsAny<Survey>()))
            .ReturnsAsync((Survey s) => s);

        var command = new CreateSurvey.Command
        {
            Title = "Test Survey",
            Questions = new List<CreateQuestionRequest>
            {
                new() { Text = "Q1", Type = "FreeText", Order = 1 }
            }
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.Equal("Test Survey", result.Title);
        Assert.Single(result.Questions);
    }
}
```

### Adding an Anonymity Contract Test

1. Identify the privacy guarantee you want to verify.
2. Create a test in `tests/Candour.Anonymity.Tests/`.
3. Test the guarantee at the architectural level -- assert that data structures, APIs, or middleware enforce the invariant.

### Adding a Playwright Test

1. Create a new `.spec.ts` file in `tests/e2e/tests/`.
2. Use Playwright's `page` fixture for browser automation.
3. Target elements by accessible roles and text content rather than CSS selectors where possible.
4. Set appropriate timeouts for Blazor WASM cold loads.

```typescript
import { test, expect } from '@playwright/test';

test('404 page shows styled error', async ({ page }) => {
  await page.goto('/nonexistent-page');
  await expect(page.getByText('Page Not Found')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Go Home' })).toBeVisible();
});
```

## Test Philosophy

Candour's testing strategy prioritizes **confidence over coverage metrics**:

1. **Anonymity contracts come first.** The `Candour.Anonymity.Tests` project is the most critical test suite. If a change breaks an anonymity guarantee, it must not ship.
2. **Integration over isolation.** Integration tests that exercise the middleware pipeline catch more real-world bugs than isolated handler tests. Both are valuable, but integration tests get priority when time is limited.
3. **Real environments over mocks (when possible).** E2E tests against live infrastructure catch deployment issues that no amount of unit testing can find. The three-tier strategy is designed so that each tier catches defects the tier below it misses.
4. **Tests document behavior.** Test names and assertions serve as executable specifications. When in doubt about how a feature works, read the tests.
