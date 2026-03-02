# Publishing & Distributing Tokens

Publishing a survey transitions it from **Draft** to **Active** status and generates a batch of blind anonymity tokens. Each token is a one-time-use link that allows a single respondent to complete the survey without any identity information being collected.

## Publishing a Survey

From the survey detail page (`/admin/survey/{id}`), click the green **Publish Survey** button. This button is only visible when the survey is in Draft status.

Publishing performs the following actions:

1. Generates a batch of HMAC-SHA256 blind tokens (default: 100 tokens).
2. Changes the survey status from **Draft** to **Active**.
3. Displays a success card with the token count.

!!! warning "Publishing Is Irreversible"
    Once a survey is published, it cannot be returned to Draft status. Respondents with valid tokens can begin submitting responses immediately. Double-check your survey title, questions, and settings before publishing.

## Token Distribution

After publishing, a success card appears with the message **"Survey Published!"** and the number of tokens generated.

![Published survey with token list](../assets/screenshots/survey-published-tokens.png)

### Viewing Tokens

Click the **Show Tokens** button to expand the token list. Each token is displayed as a full URL in the format:

```
https://app.candour.example/survey/{survey-id}?t={token}
```

!!! note "Token Display Limit"
    The token list displays the first 20 tokens in the UI. If more than 20 tokens were generated, a message indicates how many additional tokens are available. Use **Copy All Links** to retrieve the complete set.

### Copying Individual Links

Each token row includes a clipboard copy button. Click it to copy that single survey link to your clipboard. You can then paste it into an email, chat message, or other distribution channel.

### Copying All Links

Click the **Copy All Links** button to copy every generated token link to your clipboard, separated by newlines. This is the recommended approach for bulk distribution -- paste the full list into a spreadsheet or mail merge tool.

!!! tip "Distribution Best Practices"
    - Send each respondent exactly **one** unique link. Sharing the same link with multiple people means only the first person to use it can respond.
    - Do **not** publish token links in a public channel where they could be claimed by unintended recipients.
    - Keep a record of how many tokens you distributed. The difference between distributed tokens and submitted responses gives you a response rate estimate.

### What Respondents See

When a respondent opens their token link, they do not land directly on the survey form. Instead, they first see a **consent gate**.

#### Consent Gate

![Consent gate displayed before the survey](../assets/screenshots/consent-gate.png)

The consent gate is a card titled **"Before you begin"** that informs the respondent:

- **Who will see the results** -- The names of the admin users who have access to aggregate results are listed explicitly.
- **What data is collected** -- Only their answers are stored. No identity information, IP addresses, or browser fingerprints are recorded.
- **What is NOT collected** -- The consent text explicitly states that individual responses will not be visible to anyone.

The respondent must click **"I understand, begin survey"** to proceed to the survey form.

!!! note "Admin Names on the Consent Screen"
    The consent gate displays the admin names configured for the survey. This is visible on the survey detail page as well, under the label "Consent screen shows: admin1, admin2." Respondents can make an informed decision about participation based on who will see the aggregated data.

## Token Security

Tokens are designed to protect respondent anonymity at a cryptographic level:

| Property | Mechanism |
|----------|-----------|
| **One-time use** | After submission, the token's hash is stored in a `usedTokens` collection. Subsequent validation of the same token returns a rejection. |
| **Unlinkable** | There is no foreign key between `usedTokens` and `responses`. Even with database access, it is impossible to determine which response came from which token. |
| **Blind generation** | Tokens are generated using HMAC-SHA256 with a batch secret. The secret is stored in Azure Key Vault and is not accessible to admins. |
| **Non-guessable** | Tokens are cryptographically random. Brute-forcing a valid token is computationally infeasible. |

## Next Steps

- [Viewing Results](viewing-results.md) -- Load aggregate results once the anonymity threshold is met.
