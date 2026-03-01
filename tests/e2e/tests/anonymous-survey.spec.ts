import { test, expect } from '@playwright/test';

const surveyId = process.env.TEST_SURVEY_ID;

test.describe('Anonymous Survey Submission', () => {
  test.skip(!surveyId, 'TEST_SURVEY_ID not configured');

  test('survey form loads without authentication', async ({ page }) => {
    await page.goto(`/survey/${surveyId}`);

    // Blazor WASM may take time to load
    await page.waitForLoadState('networkidle');

    // Survey should display without requiring login
    await expect(page.locator('text=Submit Anonymously')).toBeVisible({ timeout: 30_000 });
  });

  test('invalid survey shows error message', async ({ page }) => {
    await page.goto('/survey/00000000-0000-0000-0000-000000000000');
    await page.waitForLoadState('networkidle');

    // Should show a graceful error, not a crash
    const errorVisible = await page.locator('.mud-alert-text-error').isVisible()
      || await page.locator('text=not found').isVisible();
    expect(errorVisible).toBeTruthy();
  });
});
