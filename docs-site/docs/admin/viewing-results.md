# Viewing Results

Once a survey has collected enough responses to meet its anonymity threshold, you can view aggregate results, monitor engagement metrics, and export response data as CSV.

## Loading Aggregate Results

From the survey detail page (`/admin/survey/{id}`), click the **Load Aggregate Results** button in the Results section.

![Aggregate results table showing option counts and percentages](../assets/screenshots/aggregate-results.png)

### Results Display

When results load successfully, the page displays:

- **Total Responses** -- A header showing the count of all submitted responses.
- **Per-question result cards** -- One card per survey question, each containing:

| Question Type | Result Format |
|---------------|---------------|
| **Multiple Choice** | A table with columns: **Option**, **Count**, **Percentage**. Each row shows how many respondents selected that option and what fraction of the total it represents. |
| **Yes / No** | Same table format as Multiple Choice, with rows for "Yes" and "No." |
| **Rating** | An **Average Rating** displayed as a decimal out of 5 (e.g., `3.8 / 5`). |
| **Free Text** | A **shuffled** list of text responses. The order is randomized using a cryptographically secure random number generator (CSPRNG) to prevent ordering-based deanonymization. |
| **Matrix** | Option counts and percentages per scale label. |

!!! note "Results Are Aggregate Only"
    Candour never exposes individual responses. There is no API endpoint or UI view that shows a single respondent's complete set of answers. All result data is aggregated across the entire response pool.

## Threshold Gate

If the survey has not yet received enough responses to meet its anonymity threshold, clicking **Load Aggregate Results** displays a warning instead of data.

![Threshold gate warning showing insufficient responses](../assets/screenshots/threshold-gate.png)

The warning message reads:

> "Insufficient responses. Need {threshold}, have {count}."

For example, if the anonymity threshold is 5 and only 2 responses have been submitted, the message reads: *"Insufficient responses. Need 5, have 2."*

!!! warning "The Threshold Protects Respondents"
    The threshold gate is not a cosmetic feature -- it is an architectural privacy control. When only a few people have responded, an admin could potentially identify individuals through process of elimination. The threshold ensures that results are only visible when the respondent pool is large enough to provide meaningful anonymity.

The threshold is enforced at both the API level and the UI level:

- The API endpoint `GET /api/surveys/{id}/results` returns an error message when the threshold is not met.
- The UI interprets this response and displays the warning alert.
- There is no way to bypass the threshold through the API or direct database queries at the application level.

## Engagement Metrics

When a survey has been viewed at least once by respondents, the survey detail page displays an **Engagement** panel above the questions section.

The engagement panel shows:

| Metric | Description |
|--------|-------------|
| **Views** | The `ViewCount` field. This counts successful token validations -- each increment represents a respondent who unlocked the survey via the consent gate. |
| **Responses** | The total number of submitted responses. This appears only after aggregate results have been loaded. |

!!! tip "Views vs. Responses"
    Comparing Views and Responses gives you a rough conversion rate. A large gap between views and responses may indicate that respondents are dropping off at the consent gate or abandoning the survey form. Consider simplifying the survey or adjusting the consent messaging if drop-off is high.

## CSV Export

Click the **Export CSV** button next to the Load Aggregate Results button to download response data as a CSV file.

![Export CSV button alongside aggregate results](../assets/screenshots/csv-export.png)

### Export Behavior

- The CSV file is named `{survey-title}-responses.csv`.
- Rows are **shuffled** using a CSPRNG before export. Responses do not appear in submission order, preventing temporal correlation attacks.
- The export endpoint (`GET /api/surveys/{id}/export`) enforces:
    - **Admin authentication** -- Only authenticated admin users can trigger an export.
    - **Anonymity threshold** -- The threshold gate applies to exports as well. If the response count is below the threshold, the export returns an error message instead of data.

!!! note "Export Error Handling"
    If the export fails (e.g., due to an unmet threshold), a warning alert appears below the Export CSV button with the error message. The button shows a loading spinner while the export is in progress.

### What the CSV Contains

The exported CSV includes one row per response, with columns for each question. The data reflects the same aggregate information available in the UI -- individual rows cannot be linked to specific respondents because:

1. No identity fields are stored in the response records.
2. Timestamps have jitter applied before storage.
3. Row order is randomized on each export.

## Consent Screen Indicator

The survey detail page includes a line below the survey settings showing which admin names appear on the respondent consent gate:

> Consent screen shows: admin1, admin2

This allows you to verify what respondents see before they begin the survey.

## Next Steps

- [Dashboard](dashboard.md) -- Return to the survey list.
- [Creating Surveys](creating-surveys.md) -- Create a new survey.
