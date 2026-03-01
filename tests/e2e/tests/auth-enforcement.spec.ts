import { test, expect } from '@playwright/test';

test.describe('Auth Enforcement', () => {
  test('admin page redirects unauthenticated users to login', async ({ page }) => {
    await page.goto('/admin');

    // Blazor WASM needs time to load, initialize, and perform auth redirect
    await page.waitForURL(
      url => url.href.includes('authentication/login') || url.href.includes('login.microsoftonline.com'),
      { timeout: 30000 }
    );
  });

  test('survey builder redirects unauthenticated users to login', async ({ page }) => {
    await page.goto('/admin/builder');

    await page.waitForURL(
      url => url.href.includes('authentication/login') || url.href.includes('login.microsoftonline.com'),
      { timeout: 30000 }
    );
  });
});
