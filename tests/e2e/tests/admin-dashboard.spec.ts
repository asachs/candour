import { test, expect } from '@playwright/test';

const email = process.env.TEST_ADMIN_EMAIL;
const password = process.env.TEST_ADMIN_PASSWORD;

test.describe('Admin Dashboard', () => {
  test.skip(!email || !password, 'Admin credentials not configured');

  test('admin can log in and see dashboard', async ({ page }) => {
    await page.goto('/admin');

    // Should redirect to Entra ID login
    await page.waitForURL(/login\.microsoftonline\.com/, { timeout: 15_000 });

    // Fill Entra ID login form
    await page.fill('input[name="loginfmt"]', email!);
    await page.click('input[type="submit"]');
    await page.waitForLoadState('networkidle');

    await page.fill('input[name="passwd"]', password!);
    await page.click('input[type="submit"]');

    // Handle "Stay signed in?" prompt if it appears
    const staySignedIn = page.locator('input[value="No"]');
    if (await staySignedIn.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await staySignedIn.click();
    }

    // Should redirect back to admin dashboard
    await page.waitForURL(/.*admin/, { timeout: 30_000 });

    // Dashboard should show survey table
    await expect(page.locator('text=Surveys').first()).toBeVisible({ timeout: 30_000 });
  });
});
