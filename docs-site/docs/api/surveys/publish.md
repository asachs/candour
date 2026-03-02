# POST /api/surveys/{id}/publish

Publish a draft survey and generate single-use blind tokens for distribution to respondents.

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
| `Content-Type` | No | `application/json` (only if sending a body) |

### Body

The request body is optional. If omitted, defaults apply.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `tokenCount` | integer | No | `100` | Number of single-use tokens to generate |

## Example

=== "curl"

    ```bash
    curl -X POST https://api.candour.example/api/surveys/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b/publish \
      -H "Authorization: Bearer eyJhbGciOi..." \
      -H "Content-Type: application/json" \
      -d '{ "tokenCount": 50 }'
    ```

=== "Response `200 OK`"

    ```json
    {
      "surveyId": "b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
      "shareableLink": "https://app.candour.example/survey/b3e1f7a2-8c4d-4e6f-9a1b-2d3c4e5f6a7b",
      "tokens": [
        "a8f3c1e9b2d4f6a7c8e0d1b3f5a2c4e6d8f0a1b3c5e7d9f1a3b5c7e9d0f2a4",
        "b9e4d2f0c3a5e7b8d1f2c4a6e8d0b2f4a6c8e0d2b4f6a8c0e2d4b6f8a0c2e4",
        "..."
      ]
    }
    ```

## Response

### Success (`200 OK`)

| Field | Type | Description |
|-------|------|-------------|
| `surveyId` | string (GUID) | The published survey's identifier |
| `shareableLink` | string | URL respondents visit to take the survey |
| `tokens` | string[] | List of single-use blind tokens |

### Errors

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | The `id` path parameter is not a valid GUID |
| `401 Unauthorized` | Missing or invalid Bearer token |
| `403 Forbidden` | Authenticated user is not an allowlisted admin |

## Notes

- Publishing transitions the survey status from `Draft` to `Active`.
- Each token is generated using `HMAC-SHA256(batchSecret, random_nonce)` where the batch secret is stored in Azure Key Vault.
- Tokens are single-use. Once a respondent [submits a response](../responses/submit.md), the token's SHA-256 hash is recorded and the token cannot be reused.
- The `shareableLink` points to the frontend application. Admins distribute this link along with individual tokens to respondents.

!!! danger "Token security"
    The `tokens` array is returned **only once** at publish time. Tokens are not stored in their original form -- only their SHA-256 hashes exist in the database. If tokens are lost, they cannot be recovered. Generate a new batch by re-publishing if needed.

!!! warning "Token distribution"
    Each respondent should receive exactly one token. Distributing multiple tokens to a single respondent or sharing tokens between respondents compromises the one-response-per-person guarantee.
