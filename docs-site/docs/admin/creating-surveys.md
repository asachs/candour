# Creating Surveys

The survey builder allows you to define a new survey with multiple question types, anonymity settings, and privacy controls. Surveys are created in **Draft** status and must be explicitly published before respondents can access them.

## Opening the Survey Builder

From the [dashboard](dashboard.md), click the **Create New Survey** button. This navigates to `/admin/builder`.

![Survey builder with questions configured](../assets/screenshots/survey-builder.png)

## Survey Settings

The top section of the builder contains the survey metadata and privacy configuration.

### Title and Description

| Field | Required | Description |
|-------|----------|-------------|
| **Survey Title** | Yes | A descriptive name for the survey. This is displayed to respondents on the survey form and the consent gate. |
| **Description** | No | Additional context about the survey's purpose. Displayed on the survey form below the title. |

### Privacy Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| **Anonymity Threshold** | 5 | 1 -- 1000 | The minimum number of responses that must be collected before aggregate results become visible to admins. This prevents deanonymization when the respondent pool is small. |
| **Timestamp Jitter** | 10 minutes | 0 -- 1440 | A random time offset (in minutes) applied to each response's `SubmittedAt` timestamp before storage. This prevents correlation of submission times with external activity logs. |

!!! warning "Choose Your Threshold Carefully"
    The anonymity threshold directly protects your respondents. Setting it too low (e.g., 1 or 2) means that an admin could potentially identify individual responses through process of elimination. A threshold of **5 or higher** is recommended for most use cases.

!!! tip "Timestamp Jitter Explained"
    If a respondent submits at 2:30 PM and jitter is set to 10 minutes, the stored timestamp will be randomly shifted to somewhere between 2:20 PM and 2:40 PM. This makes it impossible to correlate "who was at their desk at exactly 2:30 PM" with a specific response.

## Adding Questions

The builder starts with one empty question. Click **Add Question** to append additional questions to the survey.

Each question card contains:

- **Question text** -- The prompt displayed to respondents.
- **Type selector** -- The question format (see below).
- **Options field** -- Displayed only for Multiple Choice and Matrix types. Enter choices as comma-separated values.
- **Required checkbox** -- Whether the respondent must answer this question to submit.
- **Delete button** -- Removes the question (disabled when only one question remains).

### Question Types

Candour supports five question types:

| Type | Respondent Experience | Options Field | Result Format |
|------|----------------------|---------------|---------------|
| **Multiple Choice** | Radio buttons for single selection | Comma-separated choices (e.g., `Very Satisfied, Satisfied, Neutral, Dissatisfied`) | Option counts and percentages |
| **Free Text** | Open text input field | Not applicable | Shuffled list of anonymized text responses |
| **Rating** | Star rating (1--5 scale) | Not applicable | Average rating and distribution |
| **Yes / No** | Two radio buttons (Yes, No) | Auto-generated (`Yes`, `No`) | Option counts and percentages |
| **Matrix** | Likert-scale radio buttons | Comma-separated scale labels (e.g., `Strongly Disagree, Disagree, Neutral, Agree, Strongly Agree`) | Option counts and percentages per row |

!!! note "Options for Multiple Choice and Matrix"
    Enter options as a comma-separated list in the Options field. Leading and trailing whitespace around each option is automatically trimmed. For example, entering `Option A , Option B, Option C` produces three clean options: `Option A`, `Option B`, `Option C`.

## Creating the Survey

Once you have configured the title, settings, and questions:

1. Review all question cards to confirm the text, type, and options are correct.
2. Click the **Create Survey** button at the bottom of the page.
3. The builder validates that a title is provided. If validation fails, an error message appears above the button.
4. On success, you are redirected to the survey detail page showing the new survey in **Draft** status.

![Survey detail page showing Draft status after creation](../assets/screenshots/survey-detail-draft.png)

The survey detail page displays:

- The survey title and description.
- A **Draft** status chip (grey).
- The anonymity threshold and jitter settings.
- A card for each question showing its text, type, and options (as chips).
- A green **Publish Survey** button.

!!! tip "Draft Surveys Are Not Accessible"
    Surveys in Draft status cannot be accessed by respondents. No tokens exist yet, and the survey is not reachable via any public URL. You can safely iterate on the survey design knowing that nothing is live.

## Next Steps

- [Publishing & Distributing Tokens](publishing.md) -- Publish the draft survey and generate anonymous access tokens.
