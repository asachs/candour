# GET /api/surveys/{id}

Retrieve a single survey by ID, including all questions. This is the endpoint respondents use to load the survey form.

!!! info "Authentication"
    **Public.** No authentication required.

## Request

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | The survey's unique identifier |

### Headers

No special headers required.

### Body

No request body.

## Example

=== "curl"

    ```bash
    curl https://api.candour.example/api/surveys/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b
    ```

=== "Response `200 OK`"

    ```json
    {
      "id": "b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
      "title": "Q1 2026 Engineering Satisfaction",
      "description": "Quarterly pulse check on team morale and tooling.",
      "status": "Active",
      "anonymityThreshold": 5,
      "timestampJitterMinutes": 10,
      "createdAt": "2026-03-02T14:30:00Z",
      "adminNames": ["J. Admin"],
      "viewCount": 43,
      "questions": [
        {
          "id": "a1b2c3d4-0001-4000-8000-000000000001",
          "type": "Rating",
          "text": "How would you rate your overall job satisfaction?",
          "options": [],
          "required": true,
          "order": 0
        },
        {
          "id": "a1b2c3d4-0002-4000-8000-000000000002",
          "type": "MultipleChoice",
          "text": "Which area needs the most improvement?",
          "options": ["Tooling", "Communication", "Work-life balance", "Career growth"],
          "required": true,
          "order": 1
        },
        {
          "id": "a1b2c3d4-0003-4000-8000-000000000003",
          "type": "FreeText",
          "text": "What is one thing we should start doing?",
          "options": [],
          "required": false,
          "order": 2
        }
      ]
    }
    ```

## Response

### Success (`200 OK`)

Returns the full survey object with all questions.

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique survey identifier |
| `title` | string | Survey title |
| `description` | string | Survey description |
| `status` | string | Current status (`Draft`, `Active`, `Closed`) |
| `anonymityThreshold` | integer | Minimum responses before results are visible |
| `timestampJitterMinutes` | integer | Timestamp jitter range in minutes |
| `createdAt` | string (ISO 8601) | Creation timestamp |
| `adminNames` | string[] | Display names of survey administrators |
| `viewCount` | integer | Number of times the survey has been viewed |
| `questions` | array | Full list of question objects |

**Question object:**

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique question identifier (used as key when [submitting responses](../responses/submit.md)) |
| `type` | string | One of: `MultipleChoice`, `FreeText`, `Rating`, `Matrix`, `YesNo` |
| `text` | string | Question text displayed to respondents |
| `options` | string[] | Answer options (populated for `MultipleChoice`) |
| `required` | boolean | Whether the question must be answered |
| `order` | integer | Display order (0-indexed) |

### Errors

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | The `id` path parameter is not a valid GUID |
| `404 Not Found` | No survey exists with the given ID |

## Notes

- **Rate limiting:** 30 requests per 60 seconds per IP address. See [API Overview](../overview.md#rate-limiting) for details.
- The `viewCount` reflects how many respondents have validated a token for this survey. It is incremented by the [validate-token](../responses/validate-token.md) endpoint, not by this endpoint.
- Question IDs returned here are used as keys in the `answers` dictionary when [submitting a response](../responses/submit.md).
- The respondent's IP address is stripped by anonymity middleware before the request reaches this handler.
