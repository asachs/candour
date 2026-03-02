# Anonymity Architecture

Candour's anonymity is **structural**, not policy-based. The system cannot identify respondents because the data model has no fields to store identity information. This is a deliberate architectural constraint, not a configuration setting.

## Structural vs Policy Anonymity

Most survey tools rely on **policy-based anonymity**: the system collects identity data but promises not to look at it. A misconfiguration, a database join, or a code change can defeat that promise at any time.

Candour takes a different approach. The `SurveyResponse` entity contains exactly four fields:

- `Id` -- a random UUID (not auto-increment)
- `SurveyId` -- which survey this response belongs to
- `Answers` -- the response content as JSON
- `SubmittedAt` -- a jittered timestamp

There is no `RespondentId`. No `IpAddress`. No `UserAgent`. No `TokenReference`. These fields do not exist in the schema. Adding them would require a database migration and code changes across multiple layers. It cannot happen through normal API usage, misconfiguration, or accidental logging.

!!! danger "The distinction matters"
    Policy-based anonymity can be defeated by anyone with database access. Structural anonymity cannot be defeated without changing the database schema, the entity model, and the middleware -- changes that are visible in code review and version control.

## Protection Layers

Candour implements six layers of defence. Each layer addresses a specific attack vector independently, so a failure in one layer does not compromise the others.

| Layer | Protection | Mechanism |
|-------|-----------|-----------|
| **Network** | IP stripping | `AnonymityMiddleware` removes all IP-related headers before any handler processes the request |
| **Application** | Log sanitisation | Serilog destructuring policy excludes request bodies, IP addresses, and user agents from all log output |
| **Data Model** | Zero PII | Response entity has no identity fields; no foreign key between `UsedTokens` and `Responses` |
| **Temporal** | Timestamp jitter | Configurable random offset applied to `SubmittedAt` before storage |
| **Access** | Admin-only results | Aggregate results require an Entra ID JWT from an allowlisted admin |
| **Abuse Prevention** | Rate limiting | Cosmos DB-backed per-endpoint limits with TTL auto-cleanup |

!!! info "Defence in depth"
    No single layer is the "anonymity layer." Each layer independently prevents a category of deanonymisation attack. An attacker would need to defeat all six simultaneously to identify a respondent -- and the data model layer makes that structurally impossible regardless.

## Why This Approach

Enterprise survey tools commonly claim anonymity through access controls and admin promises. These claims have known failure modes:

1. **Database breach** exposes identity columns that exist but are "hidden" in the UI
2. **Log aggregation** correlates timestamps and IP addresses with survey submissions
3. **Admin access** allows viewing individual responses alongside respondent metadata
4. **Export features** include identity data that was "only stored for auditing"

Candour eliminates these failure modes at the architecture level. If the data does not exist, it cannot be breached, logged, correlated, or exported.

## Detail Pages

The anonymity architecture is documented across three supporting pages:

- **[Threat Model](threat-model.md)** -- The six attack vectors and how each layer mitigates them, plus residual risks
- **[Blind Token Scheme](blind-tokens.md)** -- How tokens authorize responses without linking identity to answers
- **[Engineering Mode](engineering-mode.md)** -- How respondents can verify the anonymity claim themselves
