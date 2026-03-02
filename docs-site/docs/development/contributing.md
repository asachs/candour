# Contributing

Thank you for your interest in contributing to Candour. This guide covers the development workflow, code conventions, and step-by-step instructions for common contribution tasks.

## Fork and Branch Workflow

Candour uses a fork-and-branch workflow:

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally:

    ```bash
    git clone https://github.com/your-username/candour.git
    cd candour
    ```

3. **Add the upstream remote:**

    ```bash
    git remote add upstream https://github.com/asachs/candour.git
    ```

4. **Create a feature branch** from `master`:

    ```bash
    git fetch upstream
    git checkout -b feature/your-feature-name upstream/master
    ```

5. **Make your changes**, commit, and push to your fork:

    ```bash
    git push origin feature/your-feature-name
    ```

6. **Open a pull request** against `master` on the upstream repository.

!!! tip "Keep Branches Focused"
    Each branch should address a single concern -- one feature, one bug fix, or one refactor. Small, focused PRs are easier to review and less likely to introduce regressions.

## Code Style

Candour follows standard .NET conventions with some project-specific guidelines.

### General Conventions

- **Language:** C# 12 / .NET 9 for all backend and shared projects. TypeScript for E2E tests.
- **Naming:** PascalCase for public members, `_camelCase` for private fields.
- **Async:** All I/O-bound methods are `async`/`await`. Suffix async methods with `Async`.
- **Nullability:** Nullable reference types are enabled. Use `?` annotations and avoid `null!` suppression where possible.
- **Using directives:** `global using` statements are preferred for frequently used namespaces.

### Architecture Rules

- **No upward dependencies.** `Candour.Core` must never reference `Candour.Application`, `Candour.Infrastructure`, or `Candour.Functions`.
- **No infrastructure in Application.** `Candour.Application` handlers must depend on interfaces from `Candour.Core`, never on concrete infrastructure classes.
- **Functions are thin adapters.** Azure Functions in `Candour.Functions/Functions/` should contain minimal logic -- parse the request, send a MediatR command/query, and return the HTTP response.
- **Document/Entity separation.** Cosmos DB documents in `Candour.Infrastructure.Cosmos/Documents/` are distinct types from domain entities in `Candour.Core/Entities/`. Mapping is explicit.

### File Organization

- One class per file (exceptions: command + handler pairs in `Candour.Application`).
- File names match the primary type name.
- Tests mirror the source project structure with a `Tests` suffix.

### Formatting

The project does not currently include an `.editorconfig`. Follow the conventions observed in existing files. Key points:

- 4-space indentation (no tabs) for C# files.
- Braces on their own line for type definitions, same line for control flow (Allman style for types, K&R for blocks is acceptable -- match the surrounding code).
- UTF-8 encoding, LF line endings preferred.

## Adding a New API Endpoint

This walkthrough covers adding a new API endpoint end-to-end, following Candour's clean architecture pattern.

### Step 1: Define the Domain Interface (if needed)

If your endpoint requires new data access, add a method to the appropriate repository interface in `Candour.Core/Interfaces/`.

```csharp
// src/Candour.Core/Interfaces/ISurveyRepository.cs
public interface ISurveyRepository : IRepository<Survey>
{
    // Existing methods...
    Task<Survey?> GetBySlugAsync(string slug);  // New method
}
```

### Step 2: Add the MediatR Command or Query

Create a new file in the appropriate directory under `src/Candour.Application/`. Follow the convention of defining the command/query record and its handler in the same file.

```csharp
// src/Candour.Application/Surveys/GetSurveyBySlug.cs
using MediatR;

namespace Candour.Application.Surveys;

public static class GetSurveyBySlug
{
    public record Query(string Slug) : IRequest<SurveyDto?>;

    public class Handler : IRequestHandler<Query, SurveyDto?>
    {
        private readonly ISurveyRepository _repo;

        public Handler(ISurveyRepository repo) => _repo = repo;

        public async Task<SurveyDto?> Handle(Query request, CancellationToken ct)
        {
            var survey = await _repo.GetBySlugAsync(request.Slug);
            return survey is null ? null : SurveyDto.From(survey);
        }
    }
}
```

### Step 3: Implement the Repository Method

Add the implementation in the Cosmos DB repository:

```csharp
// src/Candour.Infrastructure.Cosmos/Data/SurveyRepository.cs
public async Task<Survey?> GetBySlugAsync(string slug)
{
    // Cosmos DB query implementation
}
```

### Step 4: Create the Azure Function

Add a new function class in `src/Candour.Functions/Functions/`:

```csharp
// src/Candour.Functions/Functions/GetSurveyBySlugFunction.cs
public class GetSurveyBySlugFunction
{
    private readonly IMediator _mediator;

    public GetSurveyBySlugFunction(IMediator mediator) => _mediator = mediator;

    [Function("GetSurveyBySlug")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "surveys/by-slug/{slug}")]
        HttpRequestData req,
        string slug)
    {
        var result = await _mediator.Send(new GetSurveyBySlug.Query(slug));

        if (result is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }
}
```

### Step 5: Add the Shared DTO (if needed)

If the endpoint introduces a new request or response shape, add it to `src/Candour.Shared/Contracts/`.

### Step 6: Write Tests

At minimum, add:

1. **Handler test** in `tests/Candour.Application.Tests/` -- test the business logic with mocked repositories.
2. **Integration test** in `tests/Candour.Functions.Integration.Tests/` -- test the endpoint through the HTTP pipeline.

!!! warning "Tests Are Required"
    Pull requests without tests for new functionality will not be accepted. See the [Testing](testing.md) guide for conventions and examples.

## Adding a New Question Type

Candour's question type system is designed to be extensible. Here is how to add a new type end-to-end.

### Step 1: Add the Type to the Enum

```csharp
// src/Candour.Core/Enums/QuestionType.cs
public enum QuestionType
{
    MultipleChoice,
    FreeText,
    Rating,
    YesNo,
    Matrix,
    Ranking    // New type
}
```

### Step 2: Update the Survey Builder UI

Add a new option to the type selector dropdown in `src/Candour.Web/Pages/Admin/Builder.razor`:

```html
<MudSelectItem Value="@("Ranking")">Ranking</MudSelectItem>
```

Add any type-specific UI (e.g., option input fields) in the question card's conditional rendering block.

### Step 3: Update the Survey Form UI

In the respondent-facing survey form component, add rendering logic for the new question type. Handle how the respondent interacts with it (e.g., drag-and-drop ordering for a Ranking type).

### Step 4: Update Result Aggregation

In the aggregate results handler (`src/Candour.Application/`), add logic to process and summarize responses of the new type.

### Step 5: Update the Survey Detail UI

In `src/Candour.Web/Pages/Admin/SurveyDetail.razor`, add result display logic for the new type in the results section.

### Step 6: Add Tests

1. **Core tests:** Verify the enum value exists and serializes correctly.
2. **Handler tests:** Test creation and aggregation with the new type.
3. **Anonymity tests:** If the new type handles data differently, verify it maintains all anonymity guarantees.

## Pull Request Guidelines

### Before Submitting

- [ ] All existing tests pass: `dotnet test`
- [ ] New functionality has corresponding tests
- [ ] No new compiler warnings
- [ ] Code follows existing conventions (formatting, naming, architecture)
- [ ] Commit messages are descriptive and focused

### PR Description

Include the following in your pull request description:

1. **What** -- A clear summary of the change.
2. **Why** -- The motivation or issue being addressed.
3. **How** -- A brief description of the approach taken.
4. **Testing** -- How the change was tested (which test files were added/modified).

### Review Process

- All PRs require at least one approving review.
- CI must pass (build + all test tiers).
- Anonymity contract tests (`Candour.Anonymity.Tests`) must all pass. A failure in this project blocks the PR regardless of other test results.

!!! warning "Anonymity-Sensitive Changes"
    Any change that touches response handling, token validation, middleware, or data storage should be reviewed with extra scrutiny for anonymity implications. When in doubt, add a test to `Candour.Anonymity.Tests` that asserts the privacy guarantee you believe your change preserves.

### What Gets Rejected

- PRs that break anonymity contract tests.
- PRs that add infrastructure dependencies to `Candour.Core` or `Candour.Application`.
- PRs without tests for new functionality.
- PRs that store PII in response records (IP addresses, user agents, token values, session identifiers).
