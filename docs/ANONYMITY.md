# Anonymity Architecture

## Threat Model

Candour's anonymity design addresses five attack vectors through layered defences:

```mermaid
block-beta
  columns 1
  block:L1["Layer 1: Network"]
    A["IP Stripping<br/>AnonymityMiddleware removes all<br/>IP-related headers before processing"]
  end
  block:L2["Layer 2: Application"]
    B["Log Sanitisation<br/>Serilog policy excludes request bodies,<br/>IPs, and user agents from all output"]
  end
  block:L3["Layer 3: Data Model"]
    C["Zero PII<br/>Response entity has no identity fields.<br/>No FK between UsedTokens and Responses"]
  end
  block:L4["Layer 4: Temporal"]
    D["Timestamp Jitter<br/>Configurable random offset<br/>applied before storage"]
  end
  block:L5["Layer 5: Access"]
    E["Admin-Only Results<br/>Aggregate results require<br/>Entra ID JWT from allowlisted admin"]
  end

  style L1 fill:#e8f5e9,stroke:#2e7d32,color:#1b5e20
  style L2 fill:#e3f2fd,stroke:#1565c0,color:#0d47a1
  style L3 fill:#fce4ec,stroke:#c62828,color:#b71c1c
  style L4 fill:#fff3e0,stroke:#e65100,color:#bf360c
  style L5 fill:#f3e5f5,stroke:#6a1b9a,color:#4a148c
```

1. **Database breach** — Attacker gains read access to the database. Response records contain zero PII fields, so individual responses cannot be attributed.

2. **Server-side correlation** — The system itself cannot link responses to respondents. UsedTokens and Responses tables have no foreign key relationship.

3. **Timing analysis** — Configurable timestamp jitter (default +/-10 minutes) prevents ordering correlation.

4. **Network-level identification** — AnonymityMiddleware strips IP addresses and related headers before any handler processes the request.

5. **Log analysis** — Serilog destructuring policy excludes request bodies, IP addresses, and user agents from all log output.

## Blind Token Scheme

```mermaid
sequenceDiagram
    participant Admin
    participant API as Candour API
    participant KV as Key Vault
    participant DB as Cosmos DB

    Note over Admin,DB: 1 · Token Generation (Publish)
    Admin->>API: POST /surveys/{id}/publish
    API->>KV: Generate 256-bit batch secret
    KV-->>API: Batch secret stored
    API->>API: For each recipient:<br/>token = HMAC-SHA256(secret, nonce)
    API-->>Admin: Token URLs:<br/>/survey/{id}?t={token}

    Note over Admin,DB: 2 · Response Submission
    Actor R as Respondent
    Admin->>R: Distribute token URL
    R->>API: POST /surveys/{id}/responses<br/>+ token
    API->>API: tokenHash = SHA256(token)
    API->>DB: Check UsedTokens for tokenHash
    alt Token unused
        API->>DB: Store tokenHash in UsedTokens
        API->>DB: Store answers in Responses<br/>(separate, unlinked)
        API-->>R: 200 OK — Response recorded
    else Token already used
        API-->>R: 409 Conflict — Token consumed
    end
    Note over API: Original token discarded.<br/>SHA256 is one-way.

    Note over Admin,DB: 3 · No Link Possible
    Note over DB: UsedTokens table<br/>has NO foreign key<br/>to Responses table
```

1. Survey creator publishes — system generates 256-bit batch secret in Key Vault
2. Tokens generated: `HMAC-SHA256(batchSecret, random_nonce)`
3. On submission: server computes `SHA256(token)`, checks UsedTokens
4. If hash absent — store hash in UsedTokens, store answers in Responses (separate, unlinked)
5. Original token discarded — SHA256 is one-way

## Design decision: structural vs policy anonymity

Policy-based anonymity relies on configuration to exclude identity data. A code change, misconfiguration, or database join can defeat it.

Candour's Response entity has no fields for identity data. Adding PII to responses requires modifying the database schema — it cannot happen through normal API usage.

## Access Control

Aggregate results are only accessible to authenticated admin users. The API enforces this via middleware:

```mermaid
flowchart LR
    subgraph Public["Public (No Auth Required)"]
        direction TB
        A["GET /surveys/{id}<br/>View survey questions"]
        B["POST /surveys/{id}/responses<br/>Submit response"]
        C["POST /surveys/{id}/validate-token<br/>Check token validity"]
    end

    subgraph Admin["Admin Only (Entra ID JWT)"]
        direction TB
        D["GET /surveys<br/>List all surveys"]
        E["POST /surveys<br/>Create survey"]
        F["POST /surveys/{id}/publish<br/>Publish + generate tokens"]
        G["POST /surveys/{id}/analyze<br/>Run AI analysis"]
        H["GET /surveys/{id}/results<br/>View aggregate results"]
    end

    style Public fill:#e8f5e9,stroke:#2e7d32,color:#1b5e20
    style Admin fill:#fce4ec,stroke:#c62828,color:#b71c1c
```

Admin authentication requires an Entra ID JWT from an allowlisted email address. Aggregate data is only visible to authorized admins.
