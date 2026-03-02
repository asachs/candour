# GET /api/surveys/{id}/results

Retrieve aggregate results for a published survey. Results are computed across all responses and include counts, percentages, averages, and free-text answers.

!!! info "Authentication"
    **Admin required.** Requires a valid Entra ID JWT from an allowlisted admin email.

## Request

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | The survey's unique identifier |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | `Bearer <entra-id-jwt>` |

### Body

No request body.

## Example

=== "curl"

    ```bash
    curl https://api.candour.example/api/surveys/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b/results \
      -H "Authorization: Bearer eyJhbGciOi..."
    ```

=== "Response `200 OK`"

    ```json
    {
      "surveyId": "b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
      "surveyTitle": "Q1 2026 Engineering Satisfaction",
      "totalResponses": 12,
      "questions": [
        {
          "questionText": "How would you rate your overall job satisfaction?",
          "questionType": "Rating",
          "optionCounts": {},
          "optionPercentages": {},
          "freeTextAnswers": [],
          "averageRating": 3.75
        },
        {
          "questionText": "Which area needs the most improvement?",
          "questionType": "MultipleChoice",
          "optionCounts": {
            "Tooling": 5,
            "Communication": 3,
            "Work-life balance": 3,
            "Career growth": 1
          },
          "optionPercentages": {
            "Tooling": 41.67,
            "Communication": 25.0,
            "Work-life balance": 25.0,
            "Career growth": 8.33
          },
          "freeTextAnswers": [],
          "averageRating": null
        },
        {
          "questionText": "What is one thing we should start doing?",
          "questionType": "FreeText",
          "optionCounts": {},
          "optionPercentages": {},
          "freeTextAnswers": [
            "More async standups",
            "Dedicated focus time blocks",
            "Monthly tech talks from each team",
            "Better onboarding documentation",
            "Rotate on-call more evenly"
          ],
          "averageRating": null
        }
      ]
    }
    ```

## Response

### Success (`200 OK`)

| Field | Type | Description |
|-------|------|-------------|
| `surveyId` | string (GUID) | The survey's identifier |
| `surveyTitle` | string | The survey's title |
| `totalResponses` | integer | Total number of responses received |
| `questions` | array | Aggregate results per question |

**Question result object:**

| Field | Type | Description |
|-------|------|-------------|
| `questionText` | string | The question text |
| `questionType` | string | Question type (`MultipleChoice`, `FreeText`, `Rating`, `YesNo`, `Matrix`) |
| `optionCounts` | object | Map of option text to response count (for `MultipleChoice` and `YesNo`) |
| `optionPercentages` | object | Map of option text to percentage of total responses |
| `freeTextAnswers` | string[] | List of all free-text answers (for `FreeText` questions) |
| `averageRating` | number or null | Mean rating value (for `Rating` questions), `null` for other types |

### Errors

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | The `id` path parameter is not a valid GUID |
| `401 Unauthorized` | Missing or invalid Bearer token |
| `403 Forbidden` | Authenticated user is not an allowlisted admin, **or** the anonymity threshold has not been met |

=== "Threshold not met"

    ```json
    {
      "error": "Anonymity threshold not met. At least 5 responses are required."
    }
    ```

## Notes

- **Anonymity threshold:** Results are blocked with `403 Forbidden` until the number of responses meets or exceeds the survey's configured `anonymityThreshold`. This prevents de-anonymization when only a small number of responses exist.
- Free-text answers are returned in a shuffled order to prevent correlation with submission timestamps.
- Results are computed on each request rather than cached, ensuring they always reflect the latest submissions.
- The `optionPercentages` values are calculated as `(optionCount / totalResponses) * 100` and may not sum to exactly 100 due to floating-point arithmetic.

!!! warning "Anonymity threshold"
    If fewer responses have been submitted than the survey's `anonymityThreshold`, this endpoint returns `403 Forbidden` regardless of the caller's admin status. This is a privacy safeguard, not an authorization failure.
