# Anonymity Architecture

## Threat Model

Candour's anonymity design protects against:

1. **Database breach** — Attacker gains read access to the database. Response records contain zero PII fields, so individual responses cannot be attributed.

2. **Server-side correlation** — The system itself cannot link responses to respondents. UsedTokens and Responses tables have no foreign key relationship.

3. **Timing analysis** — Configurable timestamp jitter (default ±10 minutes) prevents ordering correlation.

4. **Network-level identification** — AnonymityMiddleware strips IP addresses and related headers before any handler processes the request.

5. **Log analysis** — Serilog destructuring policy excludes request bodies, IP addresses, and user agents from all log output.

## Blind Token Scheme

1. Survey creator publishes → system generates 256-bit batch secret
2. Tokens generated: `HMAC-SHA256(batchSecret, random_nonce)`
3. On submission: server computes `SHA256(token)`, checks UsedTokens
4. If hash absent → store hash in UsedTokens, store answers in Responses (separate, unlinked)
5. Original token discarded — SHA256 is one-way

## Key Design Decision: Architectural vs Policy Anonymity

Most survey tools implement anonymity as a configuration option — "don't store identifying info." This is **policy-based** anonymity. A code change, misconfiguration, or database join can defeat it.

Candour implements **architectural** anonymity — the Response entity literally has no fields for identity data. No code change within the normal API surface can add PII to responses without modifying the database schema.
