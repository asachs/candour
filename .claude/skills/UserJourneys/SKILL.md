---
name: UserJourneys
description: Captures and anonymises user journey screenshots for Candour docs. USE WHEN user journeys, capture screenshots, update journey docs, retake screenshots, regenerate USER-JOURNEYS.md.
version: 1.0.0
compatibility:
  claude_code: ">=2.1.38"
  requires:
    - playwright
---

# UserJourneys

Automates the capture of 13 screenshots across 9 user journeys for the Candour anonymity-first survey platform. Handles PII redaction (names, hostnames) at the DOM level before screenshots, and regenerates `docs/USER-JOURNEYS.md`.

## Prerequisites

- Playwright MCP server connected (browser automation)
- Candour frontend deployed to Azure Static Web Apps
- Admin user authenticated via Entra ID (for admin screenshots)
- API deployed and accessible

## Workflow Routing

| Workflow | Trigger | File |
|----------|---------|------|
| **CaptureJourneys** | "capture screenshots" OR "user journeys" OR "retake screenshots" OR "update journey docs" | `Workflows/CaptureJourneys.md` |

## Examples

**Example 1: Full screenshot capture**
```
User: "Capture user journey screenshots"
-> Invokes CaptureJourneys workflow
-> Navigates to each page, redacts PII, takes 13 screenshots
-> Writes docs/USER-JOURNEYS.md with all references
```

**Example 2: Refresh after UI changes**
```
User: "Retake the journey docs"
-> Invokes CaptureJourneys workflow
-> Re-captures all screenshots with current UI
-> Updates docs/USER-JOURNEYS.md
```
