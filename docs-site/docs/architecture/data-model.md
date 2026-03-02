# Data Model

Candour uses Azure Cosmos DB (serverless) as its sole data store. The data model is designed around one governing constraint: **response records contain zero identity fields**. Anonymity is not enforced by access control or policy -- it is enforced by the absence of fields that could identify a respondent.

## Entities

### Survey

The survey entity represents a survey created by an admin. It holds the survey definition, configuration, and metadata.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Id` | `Guid` | Auto-generated | Unique identifier (also the Cosmos partition key) |
| `CreatorId` | `string` | `""` | Entra ID object ID of the admin who created the survey |
| `Title` | `string` | `""` | Display title shown to respondents |
| `Description` | `string` | `""` | Optional description or instructions |
| `Status` | `SurveyStatus` | `Draft` | Lifecycle state: `Draft`, `Active`, or `Closed` |
| `Settings` | `string` | `"{}"` | JSON blob for extensible survey-level settings |
| `AnonymityThreshold` | `int` | `5` | Minimum responses required before results are visible |
| `TimestampJitterMinutes` | `int` | `10` | Maximum random offset applied symmetrically (+/- this many minutes) to response timestamps before storage |
| `BatchSecret` | `string` | `""` | Base64-encoded 256-bit HMAC key for token generation (encrypted at rest via Key Vault) |
| `AdminNames` | `string` | `"[]"` | JSON array of admin display names, snapshotted at publish time |
| `ViewCount` | `int` | `0` | Incremented on each valid token validation (engagement metric) |
| `CreatedAt` | `DateTime` | `UtcNow` | Creation timestamp |
| `Questions` | `List<Question>` | `[]` | Embedded question definitions (see below) |

!!! note "SurveyStatus lifecycle"
    A survey progresses through three states:

    - **Draft** -- Editable by the admin. Not yet visible to respondents.
    - **Active** -- Published and accepting responses. Token links are valid.
    - **Closed** -- No longer accepting responses. Results remain accessible to admins.

### Question

Questions are embedded within the survey document rather than stored in a separate container. This keeps the survey definition self-contained and avoids cross-document joins.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Id` | `Guid` | Auto-generated | Unique identifier for this question |
| `SurveyId` | `Guid` | — | Parent survey reference |
| `Type` | `QuestionType` | — | Question format (see enum values below) |
| `Text` | `string` | `""` | The question text displayed to respondents |
| `Options` | `string` | `"[]"` | JSON array of answer options (for multiple choice, matrix, etc.) |
| `Required` | `bool` | `true` | Whether the respondent must answer this question |
| `Order` | `int` | — | Display order within the survey |
| `Settings` | `string` | `"{}"` | JSON blob for question-level settings |

**QuestionType enum values:**

| Value | Description |
|-------|-------------|
| `MultipleChoice` | Select one or more from a list of options |
| `FreeText` | Open-ended text input |
| `Rating` | Numeric rating scale |
| `Matrix` | Grid of rows and columns |
| `YesNo` | Binary yes/no selection |

### SurveyResponse

The response entity stores a respondent's answers. This is the core of Candour's anonymity architecture.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Id` | `Guid` | Auto-generated (random UUID) | Unique identifier -- **not** auto-increment to prevent ordering attacks |
| `SurveyId` | `Guid` | — | References the parent survey (also the Cosmos partition key) |
| `Answers` | `string` | `"{}"` | JSON blob containing the respondent's answers |
| `SubmittedAt` | `DateTime` | — | Submission timestamp, **jittered** by a random offset before storage |

!!! warning "Zero PII by design"
    The `SurveyResponse` entity deliberately contains **no identity fields**. There is no `RespondentId`, no `IpAddress`, no `UserAgent`, no `TokenReference`. This is not an oversight -- it is the entire point. The code contains an explicit comment:

    ```csharp
    // DELIBERATELY NO: RespondentId, IpAddress, UserAgent, TokenReference
    // Anonymity is enforced by the absence of these fields.
    ```

    Even if the database is fully compromised, an attacker cannot link a response to a person because the linkage data was never stored.

**Additional anonymity measures on responses:**

- **Random UUID** -- The `Id` is a random `Guid`, not a sequential identifier. Sequential IDs would allow correlation by submission order.
- **Timestamp jitter** -- `SubmittedAt` is offset by a random number of minutes (+/- `TimestampJitterMinutes`) before storage, preventing timing-based correlation.
- **No foreign key to tokens** -- There is no reference from a response back to the token that authorized it.

### UsedToken

The used token entity prevents duplicate submissions without linking tokens to responses.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `TokenHash` | `string` | `""` | SHA-256 hash of the original token (primary key). The original token is **never** stored. |
| `SurveyId` | `Guid` | — | References the parent survey (Cosmos partition key) |

!!! tip "One-way hash, no foreign key"
    The token system uses a **one-way hash** for duplicate detection. When a respondent submits a response:

    1. The system hashes their token with SHA-256.
    2. It checks whether that hash exists in the `usedTokens` container.
    3. If the hash exists, the submission is rejected as a duplicate.
    4. If not, the hash is stored and the response is accepted.

    Critically, there is **no foreign key** from `UsedToken` to `SurveyResponse`. The system can tell that a token has been used, but it cannot determine *which* response it produced. This breaks the link between identity (the token) and content (the response).

## Cosmos DB Containers

All containers are auto-created on application startup by the `CosmosDbInitializer`.

| Container | Partition Key | TTL | Unique Keys | Description |
|-----------|--------------|-----|-------------|-------------|
| `surveys` | `/id` | None | None | Each survey is its own partition. Queries for a single survey are single-partition reads. |
| `responses` | `/surveyId` | None | None | All responses for a survey share a partition, enabling efficient aggregation queries. |
| `usedTokens` | `/surveyId` | None | `/tokenHash` | Unique key policy on `tokenHash` prevents duplicate token usage at the database level. |
| `rateLimits` | `/key` | Per-document | None | Rate limit counters with TTL auto-cleanup. Documents expire when their time window closes. |

!!! info "Container-level TTL on rateLimits"
    The `rateLimits` container has `DefaultTimeToLive` set to `-1`, which enables TTL at the container level but defers to per-document `ttl` values. Each rate limit entry sets its own TTL equal to the policy's window duration, so expired counters are automatically garbage-collected by Cosmos DB without a cleanup process.

## Why No Foreign Keys?

Cosmos DB is a document database -- it does not support foreign key constraints, joins, or referential integrity at the database level. But in Candour's case, the absence of foreign keys is a feature, not a limitation.

**Responses have no FK to surveys** -- While `SurveyId` exists as a partition key for query efficiency, there is no enforced constraint. If a survey is deleted, orphaned responses are harmless anonymous data.

**UsedTokens have no FK to responses** -- This is the most important design decision. A foreign key from `UsedToken` to `SurveyResponse` would create a link between the token (which is derived from the respondent's identity in the distribution list) and the response content. By omitting this link, the system ensures that even with full database access, an attacker cannot correlate who submitted which response.

**Questions are embedded, not referenced** -- Questions live inside the survey document as an embedded array. This eliminates the need for a separate container and any cross-document references. A single read of the survey document returns the complete survey definition.
