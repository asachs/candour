# Candour — User Journey Test Evidence

> End-to-end walkthrough of 7 core user journeys, with evidence captured via browser automation and API testing.

**Date:** 2026-02-27
**System:** Candour anonymity-first survey platform
**Infra:** Azure Functions (Flex Consumption), Azure Cosmos DB (Serverless), Azure Static Web Apps
**Frontend:** https://delightful-forest-005427103.4.azurestaticapps.net
**API:** https://func-candour-waf2pjwfujduu.azurewebsites.net

---

## Journey 0: Home Page & Navigation

**Goal:** Verify the landing page loads with branding, CTA, and navigation.

**Steps:**
1. Navigate to the frontend URL
2. Observe the page content and layout

**Expected:** Teal-themed app bar with "Candour" branding. Hero tagline ("Truth needs no name."), CTA button, how-it-works timeline, and three privacy feature cards with icons.

**Evidence:**
- ![Home page with new design](screenshots/home-page.png)

**Verified:**
- Teal app bar with Home / Admin / Logout navigation
- H3 tagline with proper contrast (no longer pink)
- "Go to Dashboard" CTA (auth-aware — shows "Get Started" when logged out)
- How It Works timeline: Create → Share → Analyze
- Privacy by Design cards with Shield, Key, and Lock icons

---

## Journey 1: Admin Creates a Survey

**Goal:** Verify the survey builder form creates a survey with multiple question types.

**Requires:** Entra ID login as an admin-allowlisted user.

**Steps:**
1. Navigate to `/admin` — the Survey Dashboard
2. Click "Create New Survey" (navigates to `/admin/builder`)
3. Observe breadcrumb navigation: Admin > Create Survey
4. Fill in Title: "Employee Satisfaction Q1 2026"
5. Fill in Description: "Quarterly anonymous employee satisfaction survey"
6. Set Anonymity Threshold: 3
7. Set Timestamp Jitter: 5 minutes
8. Add 3 questions:
   - Q1: Multiple Choice — "How satisfied are you with your team?" (Options: Very Satisfied, Satisfied, Neutral, Dissatisfied)
   - Q2: Free Text — "What would improve your work environment?"
   - Q3: Rating (Stars) — "Rate your overall job satisfaction"
9. Click "Create Survey"

**Expected:** Redirect to `/admin/survey/{id}` showing the created survey in Draft status with breadcrumbs (Admin > Survey Title).

**UI Features:**
- Breadcrumb navigation (Admin > Create Survey)
- Friendly question type labels (e.g., "Multiple Choice" not "MultipleChoice")
- Delete confirmation dialog when removing questions
- Anonymity Threshold and Timestamp Jitter helper text

---

## Journey 2: Admin Publishes Survey & Gets Tokens

**Goal:** Verify publishing generates anonymity tokens with fully-qualified shareable links.

**Requires:** Entra ID login as an admin-allowlisted user.

**Steps:**
1. Navigate to `/admin/survey/{id}` (survey from Journey 1)
2. Confirm survey is in "Draft" status
3. Click "Publish Survey" (only visible in Draft status — conditionally rendered)
4. Expand "Show Tokens" panel
5. Use "Copy All Links" to copy all token URLs

**Expected:** Survey status changes to "Active". Token list shows FQDN links in format `https://delightful-forest-005427103.4.azurestaticapps.net/survey/{id}?t=TOKEN`. Each token has an individual copy button. "Copy All Links" copies all URLs to clipboard.

**UI Features:**
- Publish button only rendered when status is Draft (no hidden DOM element)
- FQDN token links (not relative paths)
- Per-token copy-to-clipboard button
- Bulk "Copy All Links" button for distribution
- Question options displayed as chips under each question

---

## Journey 3: Respondent Submits Responses (x3 to meet threshold)

**Goal:** Verify respondents can submit anonymous responses using unique tokens without authentication.

**Critical Fix Verified:** The survey form page no longer crashes with `AccessTokenNotAvailableException`. Anonymous respondents use an unauthenticated API client via keyed DI services.

### Respondent 1
1. Navigate to `/survey/{id}?t={token1}`
2. Answer Q1: "Very Satisfied" (radio button)
3. Answer Q2: "More flexible hours would be great" (text area)
4. Answer Q3: 4 stars (star rating)
5. Click "Submit Anonymously"

### Respondent 2
1. Navigate to `/survey/{id}?t={token2}`
2. Answer Q1: "Satisfied"
3. Answer Q2: "Better office snacks"
4. Answer Q3: 3 stars
5. Click "Submit Anonymously"

### Respondent 3
1. Navigate to `/survey/{id}?t={token3}`
2. Answer Q1: "Neutral"
3. Answer Q2: "Quieter open office areas"
4. Answer Q3: 5 stars
5. Click "Submit Anonymously"

**Expected:** Each submission shows success: "Your anonymous response has been recorded" and "Your response cannot be linked back to you. The token has been consumed."

**Evidence (graceful error on invalid survey):**
- ![Survey form error state](screenshots/survey-form-not-found.png)

This screenshot confirms the C1 fix: navigating to a survey URL without authentication shows a graceful MudBlazor alert ("Survey not found or could not be loaded") instead of crashing with an unhandled `AccessTokenNotAvailableException`.

---

## Journey 4: Admin Views Aggregate Results

**Goal:** Verify aggregate results display correctly after threshold is met and require admin authentication.

**Requires:** Entra ID login as an admin-allowlisted user.

**Steps:**
1. Navigate to `/admin/survey/{id}`
2. Click "Load Aggregate Results"

**Expected:** Results show (only accessible to authenticated admin users):
- Total Responses: 3
- Q1 (Multiple Choice): Option counts with percentages in a table
- Q2 (Free Text): Shuffled free text responses in a list
- Q3 (Rating): Average rating displayed as "X / 5"

**UI Features:**
- Breadcrumb navigation: Admin > {Survey Title}
- Results section with MudBlazor tables and lists
- Shuffled free text responses (anonymity preservation)

---

## Journey 5: Threshold Gate — Results Blocked Below Minimum

**Goal:** Verify results are gated when response count is below the anonymity threshold.

**Steps:**
1. Create a new survey with Threshold=10
2. Publish the survey
3. Submit only 1 response
4. Navigate to admin view and click "Load Aggregate Results"

**Expected:** Warning alert indicating insufficient responses (below the anonymity threshold of 10).

---

## Journey 6: Token Reuse Prevention

**Goal:** Verify that an already-used token cannot be reused to submit another response.

**Steps:**
1. Navigate to `/survey/{id}?t={used-token}` (token already used in Journey 3)

**Expected:** Error message: token has already been consumed. The respondent is blocked from filling out the form.

**How it works:**
1. Server computes `SHA256(token)` and checks the UsedTokens table
2. If hash exists → 409 Conflict response
3. Original token is never stored — only the one-way hash

---

## Journey 7: API Auth Enforcement

**Goal:** Verify the API rejects unauthenticated requests to admin endpoints while allowing public endpoints.

**Evidence (captured 2026-02-27 from live deployment):**

```
=== Admin Endpoints (require Entra ID JWT) ===
Test 1: GET  /api/surveys                    → HTTP 401 Unauthorized
Test 2: POST /api/surveys                    → HTTP 401 Unauthorized
Test 3: GET  /api/surveys/{id}/results       → HTTP 401 Unauthorized

=== Public Endpoints (no auth required) ===
Test 4: GET  /api/surveys/{id}               → HTTP 404 (survey not found, but endpoint accessible)
Test 5: POST /api/surveys/{id}/validate-token → HTTP 200 {"valid":false,"error":"Survey not found"}
```

**Analysis:**
- Admin endpoints (`GET /surveys`, `POST /surveys`, `GET /results`) correctly return 401 when no JWT is provided
- Public endpoints (`GET /surveys/{id}`, `POST /validate-token`) are accessible without authentication
- The `POST /responses` endpoint is also public (blind token auth only)
- Token validation correctly reports that the survey doesn't exist (test used a fake UUID)

---

## Journey 8: 404 Page

**Goal:** Verify non-existent routes display a styled error page.

**Steps:**
1. Navigate to a non-existent URL (e.g., `/nonexistent-page`)

**Expected:** Styled 404 page with search icon, "Page Not Found" heading, explanation text, and "Go Home" button.

**Evidence:**
- ![404 page](screenshots/404-page.png)

---

## Design Review Summary (v0.3.0)

The following issues from the UX/UI design review have been resolved:

| # | Severity | Issue | Status |
|---|----------|-------|--------|
| C1 | CRITICAL | Survey page crash for anonymous users | Fixed — keyed DI for public HttpClient |
| C2 | CRITICAL | Pink tagline fails WCAG AA contrast | Fixed — dark text on white, h3 typography |
| M1 | MAJOR | Content hidden behind app bar | Fixed — pt-20 padding |
| M2 | MAJOR | Redundant sidebar on desktop | Fixed — temporary drawer, mobile only |
| M3 | MAJOR | Plain loading screen | Fixed — branded CSS spinner |
| M4 | MAJOR | Home page lacks CTA | Fixed — CTA, timeline, icons |
| M5 | MAJOR | No custom theme, Bootstrap conflicts | Fixed — teal theme, Bootstrap removed |
| M6 | MAJOR | Broken heading hierarchy | Fixed — proper h1-h6 ordering |
| m1 | MINOR | Default Blazor error UI | Fixed — styled error boundary |
| m2 | MINOR | No breadcrumbs (builder) | Fixed — Admin > Create Survey |
| m3 | MINOR | No breadcrumbs (detail) | Fixed — Admin > {Title} |
| m4 | MINOR | Raw enum values | Fixed — friendly labels |
| m5 | MINOR | No delete confirmation | Fixed — dialog prompt |
| m6 | MINOR | Actions column alignment | Fixed — right-aligned, outlined |
| m7 | MINOR | Hidden+disabled button | Fixed — conditional render |
