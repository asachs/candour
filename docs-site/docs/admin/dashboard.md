# Dashboard

The admin dashboard is your central hub for managing surveys in Candour. From here you can view all existing surveys, check their status, and create new ones.

## Accessing the Dashboard

Navigate to `https://app.candour.example/admin` in your browser. The dashboard requires authentication -- you will be redirected to your organization's Entra ID login page if you are not already signed in.

!!! note "Authentication Required"
    Only users on the admin allowlist can access the dashboard. If you see a login redirect loop or an access denied error, confirm that your email address has been added to the `Candour__Auth__AdminEmails` configuration. See the [Auth Modes](../configuration/auth-modes.md) page for details.

Once authenticated, the navigation bar displays **Home**, **Admin**, your display name, and a **Logout** button.

## Dashboard Overview

![Admin dashboard showing the survey list](../assets/screenshots/admin-dashboard.png)

The dashboard displays a **Survey Dashboard** heading, a **Create New Survey** button, and a table of all surveys you have created.

### Survey List Table

The table contains the following columns:

| Column | Description |
|--------|-------------|
| **Title** | The name of the survey as entered during creation. |
| **Status** | A color-coded chip indicating the survey's current state: **Draft** (grey), **Active** (green), or **Closed** (red). |
| **Threshold** | The anonymity threshold -- the minimum number of responses required before aggregate results become visible. |
| **Created** | The date the survey was created, formatted as `YYYY-MM-DD`. |
| **Actions** | Contextual buttons for interacting with the survey. |

### Empty State

If no surveys exist yet, the dashboard displays an informational message:

> "No surveys yet. Create your first one!"

## Quick Actions

### View a Survey

Click the **View** button in the Actions column of any survey row. This navigates to the [survey detail page](publishing.md) at `/admin/survey/{id}`, where you can inspect questions, publish the survey, distribute tokens, load results, and export data.

### Create a New Survey

Click the **Create New Survey** button at the top of the dashboard. This navigates to the [survey builder](creating-surveys.md) at `/admin/builder`.

### Publish from the Dashboard

For surveys in **Draft** status, a green **Publish** button appears directly in the Actions column alongside the View button. Clicking it publishes the survey immediately and refreshes the dashboard to reflect the new **Active** status.

!!! tip "Publish from Detail Page Instead"
    While you can publish directly from the dashboard, the [survey detail page](publishing.md) provides a richer publishing experience with token generation feedback and copy-to-clipboard functionality. Consider using the detail page when you need to distribute tokens immediately after publishing.

## Next Steps

- [Creating Surveys](creating-surveys.md) -- Build a new survey with the survey builder.
- [Publishing & Distributing Tokens](publishing.md) -- Publish a draft survey and share anonymous access links.
- [Viewing Results](viewing-results.md) -- Load aggregate results and export CSV data.
