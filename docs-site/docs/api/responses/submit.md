# POST /api/surveys/{id}/responses

Submit an anonymous response to a published survey. Requires a valid, unused blind token.

!!! info "Authentication"
    **Blind token.** The request body must include a single-use token distributed by the survey creator. No other authentication is required.

## Request

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | The survey's unique identifier |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Content-Type` | Yes | `application/json` |

### Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `token` | string | Yes | Single-use blind token |
| `answers` | object | Yes | Dictionary mapping question IDs to answer values |

The `answers` object maps each question's GUID (as a string key) to the respondent's answer (as a string value).

**Answer format by question type:**

| Question Type | Expected Answer Format | Example |
|---------------|----------------------|---------|
| `MultipleChoice` | One of the defined option strings | `"Work-life balance"` |
| `FreeText` | Any text string | `"More async standups"` |
| `Rating` | Numeric string (typically 1-5) | `"4"` |
| `YesNo` | `"Yes"` or `"No"` | `"Yes"` |
| `Matrix` | Implementation-specific | -- |

## Example

=== "curl"

    ```bash
    curl -X POST https://api.candour.example/api/surveys/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b/responses \
      -H "Content-Type: application/json" \
      -d '{
        "token": "a8f3c1e9b2d4f6a7c8e0d1b3f5a2c4e6d8f0a1b3c5e7d9f1a3b5c7e9d0f2a4",
        "answers": {
          "a1b2c3d4-0001-4000-8000-000000000001": "4",
          "a1b2c3d4-0002-4000-8000-000000000002": "Work-life balance",
          "a1b2c3d4-0003-4000-8000-000000000003": "More async standups"
        }
      }'
    ```

=== "Response `200 OK`"

    ```json
    {
      "id": "d5e6f7a8-1b2c-4d3e-9f0a-1b2c3d4e5f6a",
      "surveyId": "b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
      "answers": "{\"a1b2c3d4-0001-4000-8000-000000000001\":\"4\",\"a1b2c3d4-0002-4000-8000-000000000002\":\"Work-life balance\",\"a1b2c3d4-0003-4000-8000-000000000003\":\"More async standups\"}",
      "submittedAt": "2026-03-02T14:47:00Z"
    }
    ```

## Response

### Success (`200 OK`)

When engineering mode is enabled (default), the response body contains the exact Cosmos DB document that was stored. This allows respondents to verify that no identifying information was persisted.

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique response identifier |
| `surveyId` | string (GUID) | The survey this response belongs to |
| `answers` | string (JSON) | Serialized answers as stored in the database |
| `submittedAt` | string (ISO 8601) | Jittered submission timestamp |

!!! info "Engineering mode transparency"
    The stored document is returned so respondents can verify what was saved. Notice the document contains **no** IP address, user agent, token, respondent identity, or cookies. This is structural -- the `SurveyResponse` entity has no fields for identity data.

### Errors

!!! note "Error status codes"
    Token errors return `400 Bad Request` from this endpoint. This differs from the
    [validate-token](validate-token.md) endpoint, which always returns `200` to prevent
    HTTP status-based information leakage.

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | The `id` path parameter is not a valid GUID, or the request body is null, or the token is invalid/already used, or required answers are missing |

The `400` response includes an `error` field describing the specific issue:

=== "Token already used"

    ```json
    {
      "error": "Token has already been used"
    }
    ```

=== "Invalid token"

    ```json
    {
      "error": "Invalid token"
    }
    ```

## Notes

- **Rate limiting:** 5 requests per 60 seconds per IP address. See [API Overview](../overview.md#rate-limiting) for details.
- Each token can be used exactly once. After successful submission, the token's SHA-256 hash is recorded in the `UsedTokens` collection. The `UsedTokens` and `Responses` collections have **no foreign key relationship**, making it impossible to link a token to a specific response.
- The `submittedAt` timestamp includes a random jitter offset (configured by the survey's `timestampJitterMinutes` value), preventing timing-based correlation of responses.
- The respondent's IP address, user agent, and all other identifying headers are stripped by anonymity middleware before the request reaches this handler.

!!! danger "One-shot submission"
    Token consumption is irreversible. If a submission fails after the token is consumed, the token cannot be reused. Ensure the request body is complete and valid before submitting.
