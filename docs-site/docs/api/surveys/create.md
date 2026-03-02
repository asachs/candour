# POST /api/surveys

Create a new survey with questions.

!!! info "Authentication"
    **Admin required.** Requires a valid Entra ID JWT from an allowlisted admin email.

## Request

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | `Bearer <entra-id-jwt>` |
| `Content-Type` | Yes | `application/json` |

### Body

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `title` | string | Yes | -- | Survey title |
| `description` | string | Yes | -- | Survey description |
| `anonymityThreshold` | integer | No | `5` | Minimum responses before results are visible |
| `timestampJitterMinutes` | integer | No | `10` | Random offset range (+/-) applied to submission timestamps |
| `questions` | array | Yes | -- | List of question objects |

**Question object:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `type` | string | Yes | -- | One of: `MultipleChoice`, `FreeText`, `Rating`, `Matrix`, `YesNo` |
| `text` | string | Yes | -- | Question text displayed to respondents |
| `options` | string[] | No | `[]` | Answer options (required for `MultipleChoice`) |
| `required` | boolean | No | `true` | Whether the question must be answered |
| `order` | integer | Yes | -- | Display order (0-indexed) |

## Example

=== "curl"

    ```bash
    curl -X POST https://api.candour.example/api/surveys \
      -H "Authorization: Bearer eyJhbGciOi..." \
      -H "Content-Type: application/json" \
      -d '{
        "title": "Q1 2026 Engineering Satisfaction",
        "description": "Quarterly pulse check on team morale and tooling.",
        "anonymityThreshold": 5,
        "timestampJitterMinutes": 10,
        "questions": [
          {
            "type": "Rating",
            "text": "How would you rate your overall job satisfaction?",
            "options": [],
            "required": true,
            "order": 0
          },
          {
            "type": "MultipleChoice",
            "text": "Which area needs the most improvement?",
            "options": ["Tooling", "Communication", "Work-life balance", "Career growth"],
            "required": true,
            "order": 1
          },
          {
            "type": "FreeText",
            "text": "What is one thing we should start doing?",
            "options": [],
            "required": false,
            "order": 2
          }
        ]
      }'
    ```

=== "Response `201 Created`"

    ```json
    {
      "id": "b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
      "title": "Q1 2026 Engineering Satisfaction",
      "description": "Quarterly pulse check on team morale and tooling.",
      "status": "Draft",
      "anonymityThreshold": 5,
      "timestampJitterMinutes": 10,
      "createdAt": "2026-03-02T14:30:00Z",
      "adminNames": [],
      "viewCount": 0,
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

### Success (`201 Created`)

Returns the full survey object including server-generated IDs.

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique survey identifier |
| `title` | string | Survey title |
| `description` | string | Survey description |
| `status` | string | Survey status (`Draft` after creation) |
| `anonymityThreshold` | integer | Minimum responses before results are visible |
| `timestampJitterMinutes` | integer | Timestamp jitter range in minutes |
| `createdAt` | string (ISO 8601) | Creation timestamp |
| `adminNames` | string[] | List of admin display names |
| `viewCount` | integer | Number of times the survey has been viewed |
| `questions` | array | List of question objects with server-generated IDs |

**Question object (response):**

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique question identifier |
| `type` | string | Question type |
| `text` | string | Question text |
| `options` | string[] | Answer options |
| `required` | boolean | Whether the question is required |
| `order` | integer | Display order |

### Errors

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | Request body is null or could not be deserialized |
| `401 Unauthorized` | Missing or invalid Bearer token |
| `403 Forbidden` | Authenticated user is not an allowlisted admin |

## Notes

- Newly created surveys have status `Draft`. They must be [published](publish.md) before respondents can submit answers.
- The `anonymityThreshold` controls the minimum number of responses required before [aggregate results](../results/aggregate.md) and [CSV exports](../results/export.md) become available.
- The `timestampJitterMinutes` value is applied as a random offset (+/-) to each response's submission timestamp, preventing timing-based correlation.
- The creator's Entra ID object identifier is stored internally but never exposed in the API response.
