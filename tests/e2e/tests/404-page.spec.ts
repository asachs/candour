import { test, expect } from '@playwright/test';

test.describe('404 Page', () => {
  test('non-existent route shows styled 404 page', async ({ page }) => {
    await page.goto('/nonexistent-page-xyz');
    await page.waitForLoadState('networkidle');

    // Should show "Page Not Found" heading
    await expect(page.locator('text=Page Not Found')).toBeVisible({ timeout: 30_000 });

    // Should show "Go Home" button
    await expect(page.locator('text=Go Home')).toBeVisible();
  });

  test('Go Home button navigates to home page', async ({ page }) => {
    await page.goto('/nonexistent-page-xyz');
    await page.waitForLoadState('networkidle');

    await page.locator('text=Go Home').click();
    await page.waitForURL(/^\/$|\/$/);
  });
});
