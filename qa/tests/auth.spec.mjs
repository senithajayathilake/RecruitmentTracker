import { expect, test } from '@playwright/test';

test('an anonymous visitor is sent to sign in', async ({ page }) => {
  await page.goto('/OfferLetter/Index');

  await expect(page).toHaveURL(/\/Account\/Login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
});

test('wrong HR password is rejected', async ({ page }) => {
  await page.goto('/Account/Login');
  await page.getByLabel('Email').fill('hr@recruitment.local');
  await page.getByLabel('Password').fill('incorrect-password');
  await page.getByRole('button', { name: /sign in/i }).click();

  await expect(page.getByText('Invalid email or password.')).toBeVisible();
  await expect(page).toHaveURL(/\/Account\/Login/);
});

test('a candidate cannot access the HR offer list', async ({ page }) => {
  await page.goto('/Account/Login');
  await page.getByLabel('Email').fill('candidate@recruitment.local');
  await page.getByLabel('Password').fill('Candidate123');
  await page.getByRole('button', { name: /sign in/i }).click();

  await expect(page).toHaveURL(/\/Dashboard\/Candidate/);
  await page.goto('/OfferLetter/Index');
  await expect(page).toHaveURL(/\/Account\/AccessDenied/);
});
