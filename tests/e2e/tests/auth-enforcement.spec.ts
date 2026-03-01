import { test, expect } from '@playwright/test';

test.describe('Auth Enforcement', () => {
  test('admin page redirects unauthenticated users to login', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');

    // Should redirect to authentication/login or Entra ID
    const url = page.url();
    const isRedirected = url.includes('authentication/login')
      || url.includes('login.microsoftonline.com');
    expect(isRedirected).toBeTruthy();
  });

  test('survey builder redirects unauthenticated users to login', async ({ page }) => {
    await page.goto('/admin/builder');
    await page.waitForLoadState('networkidle');

    const url = page.url();
    const isRedirected = url.includes('authentication/login')
      || url.includes('login.microsoftonline.com');
    expect(isRedirected).toBeTruthy();
  });
});
