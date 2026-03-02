# GET /api/surveys

List all surveys. Returns summary information for every survey in the system.

!!! info "Authentication"
    **Admin required.** Requires a valid Entra ID JWT from an allowlisted admin email.

## Request

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | `Bearer <entra-id-jwt>` |

### Body

No request body.

## Example

=== "curl"

    ```bash
    curl https://api.candour.example/api/surveys \
      -H "Authorization: Bearer eyJhbGciOi..."
    ```

=== "Response `200 OK`"

    ```json
    [
      {
        "id": "b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
        "title": "Q1 2026 Engineering Satisfaction",
        "description": "Quarterly pulse check on team morale and tooling.",
        "status": "Active",
        "anonymityThreshold": 5,
        "timestampJitterMinutes": 10,
        "createdAt": "2026-03-02T14:30:00Z",
        "adminNames": ["J. Admin"],
        "viewCount": 42,
        "questions": []
      },
      {
        "id": "c4f2a8b3-9d5e-4f70-ab2c-3e4d5f6a7b8c",
        "title": "Onboarding Experience Feedback",
        "description": "How was your first 90 days?",
        "status": "Draft",
        "anonymityThreshold": 3,
        "timestampJitterMinutes": 15,
        "createdAt": "2026-02-15T09:00:00Z",
        "adminNames": ["J. Admin", "K. Manager"],
        "viewCount": 0,
        "questions": []
      }
    ]
    ```

## Response

### Success (`200 OK`)

Returns a JSON array of survey summary objects. The `questions` array is empty in the list response to reduce payload size.

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
| `questions` | array | Always empty in the list response |

### Errors

| Status | Condition |
|--------|-----------|
| `401 Unauthorized` | Missing or invalid Bearer token |
| `403 Forbidden` | Authenticated user is not an allowlisted admin |

## Notes

- This endpoint returns all surveys regardless of status. Use the `status` field to filter client-side.
- Questions are not included in the list response. Use [GET /api/surveys/{id}](get.md) to retrieve full survey details including questions.
- See [GET /api/surveys/{id}](get.md) for details on how `viewCount` is incremented.
- The `adminNames` field contains display names of users who have administrative access to the survey.
