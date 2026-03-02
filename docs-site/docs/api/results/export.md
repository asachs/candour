# GET /api/surveys/{id}/export

Export all responses for a survey as a CSV file download. The export preserves all anonymity guarantees.

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
    curl https://api.candour.example/api/surveys/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b/export \
      -H "Authorization: Bearer eyJhbGciOi..." \
      -o results.csv
    ```

=== "Response `200 OK`"

    ```csv
    SubmittedAt,How would you rate your overall job satisfaction?,Which area needs the most improvement?,What is one thing we should start doing?
    2026-03-02T14:52:00Z,4,Work-life balance,More async standups
    2026-03-02T14:38:00Z,3,Tooling,Better onboarding documentation
    2026-03-02T15:01:00Z,5,Communication,Monthly tech talks from each team
    2026-03-02T14:45:00Z,4,Tooling,Dedicated focus time blocks
    2026-03-02T14:59:00Z,2,Work-life balance,Rotate on-call more evenly
    ```

## Response

### Success (`200 OK`)

The response is a CSV file download. The following headers are set:

| Header | Value |
|--------|-------|
| `Content-Type` | `text/csv; charset=utf-8` |
| `Content-Disposition` | `attachment; filename="<generated-filename>"` |
| `Access-Control-Expose-Headers` | `Content-Disposition` |

**CSV structure:**

- The first row contains column headers.
- The first column is always `SubmittedAt` (jittered timestamp).
- Subsequent columns correspond to each question's text in order.
- Each data row represents one anonymous response.

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

- **Anonymity threshold:** Like the [aggregate results endpoint](aggregate.md), the export is blocked with `403 Forbidden` until the number of responses meets or exceeds the survey's configured `anonymityThreshold`.
- **Row shuffling:** Responses are shuffled using a cryptographically secure random number generator (CSPRNG) before CSV generation. Row order does not correspond to submission order.
- **Timestamp jitter:** The `SubmittedAt` values include the configured random offset, preventing timing-based correlation.
- **Zero PII columns:** The CSV contains only question answers and jittered timestamps. No identity fields exist to export -- no IP addresses, user agents, tokens, or respondent identifiers.
- The `Access-Control-Expose-Headers` response header ensures the `Content-Disposition` filename is accessible to browser-based clients making cross-origin requests.

!!! danger "Sensitive data"
    Although the export contains no identity data, free-text answers may contain sensitive information written by respondents. Handle exported CSV files according to your organization's data handling policies.

!!! warning "Anonymity threshold"
    If fewer responses have been submitted than the survey's `anonymityThreshold`, this endpoint returns `403 Forbidden` regardless of the caller's admin status. This is a privacy safeguard, not an authorization failure.
