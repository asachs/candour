# POST /api/surveys/{id}/validate-token

Check whether a blind token is valid for a given survey. This endpoint is used by the frontend to verify a token before displaying the survey form.

!!! info "Authentication"
    **Public.** No authentication required.

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
| `token` | string | Yes | The blind token to validate |

## Example

=== "curl"

    ```bash
    curl -X POST https://api.candour.example/api/surveys/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b/validate-token \
      -H "Content-Type: application/json" \
      -d '{ "token": "a8f3c1e9b2d4f6a7c8e0d1b3f5a2c4e6d8f0a1b3c5e7d9f1a3b5c7e9d0f2a4" }'
    ```

=== "Response `200 OK` (valid)"

    ```json
    {
      "valid": true,
      "error": null
    }
    ```

=== "Response `200 OK` (invalid)"

    ```json
    {
      "valid": false,
      "error": "Token has already been used"
    }
    ```

## Response

### Success (`200 OK`)

The endpoint always returns `200 OK` with a validation result. This prevents information leakage through HTTP status codes.

| Field | Type | Description |
|-------|------|-------------|
| `valid` | boolean | `true` if the token is valid and unused, `false` otherwise |
| `error` | string or null | Human-readable reason when `valid` is `false`, `null` when valid |

Possible `error` values:

| Error | Meaning |
|-------|---------|
| `null` | Token is valid and has not been used |
| `"Token has already been used"` | Token was consumed by a previous response submission |
| `"Invalid token"` | Token does not match any token generated for this survey |
| `"Survey is not published"` | The survey exists but has not been published yet |

### Errors

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | The `id` path parameter is not a valid GUID, or the request body is missing/empty, or the `token` field is missing |

## Notes

- **Rate limiting:** 10 requests per 60 seconds per IP address. See [API Overview](../overview.md#rate-limiting) for details.
- This endpoint does **not** consume the token. Validation is read-only. The token is only consumed when a [response is submitted](submit.md).
- Token validity is checked by computing `SHA-256(token)` and looking up the hash in the database. The raw token is never stored.
- The respondent's IP address is stripped by anonymity middleware before the request reaches this handler.

!!! warning "Timing considerations"
    Validate the token close to submission time. A token that is valid now could be consumed by another request before the respondent finishes filling out the survey.
