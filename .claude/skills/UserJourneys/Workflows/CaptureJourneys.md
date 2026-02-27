# CaptureJourneys Workflow

Captures all 13 user journey screenshots with PII redaction and regenerates `docs/USER-JOURNEYS.md`.

---

## Redaction Configuration

Before capturing any screenshots, define these text replacement rules. Apply them via DOM TreeWalker before every screenshot.

| Original | Replacement | Reason |
|----------|-------------|--------|
| The authenticated user's real name (from Entra ID) | `Admin User` | PII redaction |
| The SWA hostname (e.g. `delightful-forest-005427103.4.azurestaticapps.net`) | `app.candour.example` | Infrastructure redaction |

> **To update redaction rules:** Edit this table. The workflow reads these values at capture time.

### Redaction JavaScript

Run this on every page before taking a screenshot:

```javascript
(function redact() {
  const replacements = [
    // ADD OR MODIFY ENTRIES HERE
    [/REAL_NAME_HERE/gi, 'Admin User'],
    [/HOSTNAME_HERE/gi, 'app.candour.example']
  ];
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  while (walker.nextNode()) {
    let text = walker.currentNode.textContent;
    for (const [pattern, replacement] of replacements) {
      text = text.replace(pattern, replacement);
    }
    walker.currentNode.textContent = text;
  }
})();
```

**Before executing:** Replace `REAL_NAME_HERE` with the actual authenticated user's name (visible in the nav bar) and `HOSTNAME_HERE` with the actual SWA hostname from the browser's URL bar.

---

## Step 0: Prerequisites

1. **Verify Playwright MCP** is connected (browser_navigate, browser_snapshot, browser_take_screenshot must be available)
2. **Read `appsettings.json`** from the published output to get the frontend base URL and Azure AD config:
   - Read `/tmp/candour-web-publish/wwwroot/appsettings.json` (or wherever the latest publish output is)
   - Note the `ApiBaseUrl` and `AzureAd` settings
3. **Set up route interception** to bypass CDN-cached stale config:

```javascript
// Run via browser_run_code BEFORE navigating to any page
async (page) => {
  await page.route('**/appsettings.json', async route => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        "ApiBaseUrl": "API_URL_HERE",
        "AzureAd": {
          "Enabled": true,
          "Authority": "https://login.microsoftonline.com/TENANT_ID_HERE",
          "ClientId": "CLIENT_ID_HERE",
          "ApiScope": "api://CLIENT_ID_HERE/access_as_user"
        }
      })
    });
  });
}
```

Replace the placeholders with values from the actual `appsettings.json`.

4. **Navigate to the frontend** and verify it loads
5. **Authenticate** — Click Login, complete Entra ID flow. Confirm the nav bar shows the user's name and Admin link.
6. **Identify the user's display name** from the nav bar (needed for redaction rules)

---

## Step 1: Capture Home Page Screenshots

### Screenshot 1: `home-page.png` — Home page (logged out)

If you have an unauthenticated session available:
1. Navigate to `/`
2. Wait for content to load (look for "Truth needs no name" tagline)
3. **Run redaction script**
4. Take full-page screenshot -> `docs/screenshots/home-page.png`

> If already authenticated, skip this — reuse the existing `home-page.png` if it has no PII.

### Screenshot 2: `home-page-authenticated.png` — Home page (authenticated)

1. Navigate to `/`
2. Wait for "Go to Dashboard" CTA to appear (auth-aware)
3. **Run redaction script** (replaces name in nav bar)
4. Take full-page screenshot -> `docs/screenshots/home-page-authenticated.png`

---

## Step 2: Capture Admin Dashboard

### Screenshot 3: `admin-dashboard.png`

1. Navigate to `/admin`
2. Wait for the survey table to load
3. **Run redaction script**
4. Take full-page screenshot -> `docs/screenshots/admin-dashboard.png`

---

## Step 3: Capture Survey Builder

### Screenshot 4: `survey-builder.png`

1. Navigate to `/admin/builder`
2. Fill in demo data:
   - Title: "Employee Satisfaction Q1 2026"
   - Description: "Quarterly anonymous employee satisfaction survey"
   - Anonymity Threshold: 5
   - Timestamp Jitter: 10
3. Configure Question 1 (already present):
   - Type: Multiple Choice
   - Text: "How satisfied are you with your team?"
   - Options: "Very Satisfied, Satisfied, Neutral, Dissatisfied"
4. Click "Add Question" and configure Question 2:
   - Type: Free Text
   - Text: "What would improve your work environment?"
5. Click "Add Question" and configure Question 3:
   - Type: Rating (Stars)
   - Text: "Rate your overall job satisfaction"
6. **Run redaction script**
7. Take full-page screenshot -> `docs/screenshots/survey-builder.png`

> **Do NOT click "Create Survey"** — this screenshot captures the builder in-progress.

---

## Step 4: Capture Survey Detail (Draft)

To get a draft survey screenshot, either:
- **Option A:** Create the survey from Step 3 (click "Create Survey"), which redirects to the detail page in Draft status
- **Option B:** If a draft survey already exists, navigate to `/admin/survey/{id}`

### Screenshot 5: `survey-detail-draft.png`

1. Navigate to the draft survey's detail page
2. Confirm "Draft" status badge and green "Publish Survey" button are visible
3. **Run redaction script**
4. Take full-page screenshot -> `docs/screenshots/survey-detail-draft.png`

---

## Step 5: Capture Published Survey with Tokens

### Screenshot 6: `survey-published-tokens.png`

1. From the draft survey detail page, click "Publish Survey"
2. Wait for the success card: "Survey Published!" with "Generated N tokens"
3. Expand the token list if collapsed
4. **Run redaction script** (critical — token URLs contain the real hostname)
5. Take full-page screenshot -> `docs/screenshots/survey-published-tokens.png`

> **Important:** The token list is ephemeral — it only shows immediately after publishing. Navigating away loses it.

---

## Step 6: Capture Survey Detail with Question Chips

### Screenshot 7: `survey-detail.png`

1. Navigate to any published survey's detail page: `/admin/survey/{id}`
2. Wait for questions to load with option chips displayed
3. **Run redaction script**
4. Take full-page screenshot -> `docs/screenshots/survey-detail.png`

---

## Step 7: Capture Respondent Survey Form

### Screenshot 8: `survey-form.png`

You need a valid, unused token for a published survey.

1. Get a token URL from the published survey (Step 5) or from the API
2. Navigate to `/survey/{id}?t={token}`
3. Wait for the form to load with radio buttons / text fields
4. Optionally select an answer (e.g., click "Satisfied") for a more realistic screenshot
5. **Run redaction script**
6. Take full-page screenshot -> `docs/screenshots/survey-form.png`

### Screenshot 9: `survey-submitted.png`

1. Fill in answers for all required questions
2. Click "Submit Anonymously"
3. Wait for success message: "Your anonymous response has been recorded"
4. **Run redaction script**
5. Take full-page screenshot -> `docs/screenshots/survey-submitted.png`

### Screenshot 10: `survey-form-not-found.png`

1. Navigate to `/survey/00000000-0000-0000-0000-000000000000?t=fake`
2. Wait for error: "Survey not found or could not be loaded."
3. Take screenshot -> `docs/screenshots/survey-form-not-found.png`

---

## Step 8: Capture Aggregate Results

### Screenshot 11: `aggregate-results.png`

**Prerequisite:** The survey must have responses meeting the anonymity threshold.

If the survey from Step 5 doesn't have enough responses yet:
- Submit additional responses via the API using unused tokens:

```bash
curl -s -X POST "API_URL/api/surveys/{surveyId}/responses" \
  -H "Content-Type: application/json" \
  -d '{"token":"TOKEN_VALUE","answers":{"QUESTION_GUID":"Answer"}}'
```

Then:
1. Navigate to `/admin/survey/{id}` for the survey with sufficient responses
2. Click "Load Aggregate Results"
3. Wait for the results table to appear (Option / Count / Percentage columns)
4. **Run redaction script**
5. Take full-page screenshot -> `docs/screenshots/aggregate-results.png`

---

## Step 9: Capture Threshold Gate

### Screenshot 12: `threshold-gate.png`

1. Navigate to a survey that has 0 responses (or fewer than its threshold)
2. Click "Load Aggregate Results"
3. Wait for warning: "Insufficient responses. Need X, have Y."
4. **Run redaction script**
5. Take full-page screenshot -> `docs/screenshots/threshold-gate.png`

---

## Step 10: Capture 404 Page

### Screenshot 13: `404-page.png`

1. Navigate to `/nonexistent-page-xyz`
2. Wait for styled 404 page with search icon
3. Take screenshot -> `docs/screenshots/404-page.png`

---

## Step 11: Generate USER-JOURNEYS.md

Write or update `docs/USER-JOURNEYS.md` with the following structure. **Use `app.candour.example` for all hostnames in the prose** (not the real SWA URL).

### Template

```markdown
# Candour — User Journey Test Evidence

> End-to-end walkthrough of 9 core user journeys, with evidence captured via browser automation and API testing.

**Date:** {TODAY'S DATE}
**System:** Candour anonymity-first survey platform
**Infra:** Azure Functions (Flex Consumption), Azure Cosmos DB (Serverless), Azure Static Web Apps
**Frontend:** https://app.candour.example
**API:** https://api.candour.example

---

## Journey 0: Home Page & Navigation
[Reference screenshots: home-page.png, home-page-authenticated.png]
[Document what was verified]

## Journey 1: Admin Creates a Survey
[Reference screenshots: admin-dashboard.png, survey-builder.png, survey-detail-draft.png]

## Journey 2: Admin Publishes Survey & Gets Tokens
[Reference screenshot: survey-published-tokens.png]

## Journey 3: Respondent Submits Anonymous Response
[Reference screenshots: survey-form.png, survey-submitted.png, survey-form-not-found.png]

## Journey 4: Admin Views Aggregate Results
[Reference screenshot: aggregate-results.png]

## Journey 5: Threshold Gate — Results Blocked Below Minimum
[Reference screenshot: threshold-gate.png]

## Journey 6: Token Reuse Prevention
[Document the server-side behavior — no screenshot needed]

## Journey 7: API Auth Enforcement
[Document curl test results — no screenshot needed]

## Journey 8: 404 Page
[Reference screenshot: 404-page.png]

---

## Screenshot Inventory
[Table mapping each PNG to its journey]

---

## Design Review Summary
[Table of resolved UX issues from C1 through m7]
```

Use the existing `docs/USER-JOURNEYS.md` as reference for the exact prose, verified items, and design review table. Update the date and any details that changed.

---

## Step 12: Verify

1. List all files in `docs/screenshots/` — confirm all 13 PNGs exist
2. Check that no filename contains PII
3. Read through the generated `docs/USER-JOURNEYS.md` and confirm:
   - All screenshot references point to existing files
   - No real hostname appears in the prose
   - No real user name appears in the prose
4. Optionally open 2-3 screenshots to visually confirm redaction worked

---

## Done

All 13 screenshots captured with PII redacted. `docs/USER-JOURNEYS.md` updated with current evidence. Ready to commit.
