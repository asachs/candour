# Engineering Mode

Candour does not ask respondents to trust a privacy policy. Instead, it shows them exactly what was stored after they submit a response. Engineering mode lets respondents verify the anonymity claim with their own eyes.

## What It Is

After a respondent submits their survey answers, engineering mode displays the exact Cosmos DB document that was persisted. The respondent can inspect every field and confirm that no identifying information was recorded.

This transparency is safe because the `SurveyResponse` entity contains zero identity fields by design. Showing the stored document reveals nothing sensitive -- it only proves the anonymity guarantee.

![Engineering mode panel](../assets/screenshots/engineering-mode.png)

## The Stored Document

When engineering mode is enabled, the respondent sees the actual document in the database:

```json
{
  "Id": "a1b2c3d4-...",
  "SurveyId": "e5f6g7h8-...",
  "Answers": "{\"q1\":\"Yes\",\"q2\":\"Great team\"}",
  "SubmittedAt": "2026-03-02T10:15:00Z"
}
```

Four fields. A random UUID, the survey identifier, the answers as JSON, and a jittered timestamp. Nothing else.

## What Was NOT Stored

Alongside the stored document, an explicit list shows the respondent what the system deliberately excluded:

| Excluded Data | Why It Matters |
|--------------|---------------|
| **IP address** | Cannot determine the respondent's network location |
| **User agent** | Cannot determine the respondent's browser or device |
| **Survey token** | Cannot link this response to the token used to submit it |
| **Respondent identity** | No user ID, email, or name is associated with the response |
| **Cookies** | No session or tracking cookies are set on respondent routes |

!!! info "These fields do not exist"
    This is not a list of fields that were redacted or hidden. These fields do not exist in the `SurveyResponse` entity. There is no code path that could store them, no configuration that could enable them, and no database column to receive them.

## Why This Matters

Most anonymity claims require trust. The survey platform says "we don't store your identity" and respondents must take that at face value. Engineering mode replaces trust with verification.

A technically inclined respondent can:

1. Read the stored document and confirm it contains no identity data.
2. Cross-reference the four fields against the `SurveyResponse` entity in the public source code.
3. Inspect the `AnonymityMiddleware` to verify that IP headers are stripped before processing.
4. Verify that the `UsedTokens` table has no foreign key to the `Responses` table.

!!! note "Designed for skeptics"
    Engineering mode exists because the strongest form of trust is the ability to verify. Respondents who understand the system can confirm the anonymity claim independently. Respondents who do not can still benefit from the structural guarantee.

## Configuration

Engineering mode is controlled by the `EngineeringMode` configuration key.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EngineeringMode` | `bool` | `true` | When enabled, respondents see the stored document after submission |

!!! warning "Recommendation: keep it enabled"
    Engineering mode is enabled by default and should remain enabled in production. It is the primary mechanism for respondent trust. Disabling it removes the ability for respondents to verify anonymity but does not change the underlying data model -- the anonymity guarantee remains structural regardless of this setting.

## CSV Export Safety

For CSV export anonymity guarantees (threshold enforcement, CSPRNG row shuffling), see [Viewing Results](../admin/viewing-results.md#csv-export).
