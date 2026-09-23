import { expect, test } from '@playwright/test';
import { fileURLToPath } from 'node:url';

const fixture = fileURLToPath(new URL('../fixtures/qa-cv.pdf', import.meta.url));

function isoDate(daysFromToday) {
  const date = new Date();
  date.setDate(date.getDate() + daysFromToday);
  return date.toISOString().slice(0, 10);
}

test('HR hires a manually added candidate and issues an offer', async ({ page }) => {
  const unique = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
  const jobTitle = `QA Developer ${unique}`;
  const candidateName = `QA Candidate ${unique}`;
  const email = `qa-${unique}@example.test`;
  let candidateUrl;
  let applicationId;

  await test.step('Sign in as HR', async () => {
    await page.goto('/Account/Login');
    await page.getByLabel('Email').fill('hr@recruitment.local');
    await page.getByLabel('Password').fill('Hr12345');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/Dashboard\/HR/);
  });

  await test.step('Create a vacancy', async () => {
    await page.goto('/Vacancy/Create');
    await page.locator('#JobTitle').fill(jobTitle);
    await page.locator('#Department').fill('QA');
    await page.locator('#Location').fill('Colombo');
    await page.locator('#Salary').fill('150000');
    await page.locator('#Description').fill('Automated QA test vacancy.');
    await page.locator('#Requirements').fill('C# and Playwright.');
    await page.locator('#ApplicationDeadline').fill(isoDate(60));
    await page.getByRole('button', { name: 'Create vacancy' }).click();
    await expect(page).toHaveURL(/\/Vacancy(?:\/Index)?$/);
    await expect(page.getByText(jobTitle, { exact: true })).toBeVisible();
  });

  await test.step('Add a candidate and upload a CV', async () => {
    await page.goto('/Candidate/Create');
    await page.locator('#FullName').fill(candidateName);
    await page.locator('#Email').fill(email);
    await page.getByRole('button', { name: 'Add candidate' }).click();
    await expect(page).toHaveURL(/\/Candidate(?:\/Index)?$/);

    const candidateRow = page.getByRole('row').filter({ hasText: email });
    await expect(candidateRow).toContainText(candidateName);
    await candidateRow.getByRole('link', { name: 'View' }).click();
    await expect(page).toHaveURL(/\/Candidate\/Details\/\d+/);
    candidateUrl = page.url();

    await page.getByRole('link', { name: 'Upload CV', exact: true }).click();
    await page.locator('input[type="file"]').setInputFiles(fixture);
    await page.getByRole('button', { name: /submit cv/i }).click();
    await expect(page).toHaveURL(/\/Candidate\/Details\/\d+/);
    await expect(page.getByText('qa-cv.pdf')).toBeVisible();
  });

  await test.step('Hire the candidate for the vacancy', async () => {
    await page.getByLabel('Position to hire for').selectOption({ label: `${jobTitle} (QA)` });
    await page.getByRole('button', { name: 'Hire for this position' }).click();
    await expect(page).toHaveURL(/\/OfferLetter(?:\/Index)?$/);

    const row = page.getByRole('row').filter({ hasText: email });
    await expect(row).toContainText(jobTitle);
    await expect(row).toContainText('Not issued');
    const href = await row.getByRole('link', { name: /create offer/i }).getAttribute('href');
    applicationId = href?.match(/\/(\d+)$/)?.[1];
    expect(applicationId).toBeTruthy();
  });

  await test.step('Candidate Hired button updates the existing application', async () => {
    await page.goto(`/Application/Details/${applicationId}`);
    await page.getByRole('button', { name: 'On Hold', exact: true }).click();
    await expect(page).toHaveURL(/\/Application\/Details\/\d+/);

    await page.goto(candidateUrl);
    await page.getByRole('button', { name: 'Hired', exact: true }).click();
    await page.goto(`/Application/Details/${applicationId}`);
    await expect(page.locator('.badge').filter({ hasText: 'Hired' })).toBeVisible();
    await page.goto('/OfferLetter/Index');

    const row = page.getByRole('row').filter({ hasText: email });
    await expect(row).toContainText(jobTitle);
    await expect(row).toContainText('Not issued');
    await row.getByRole('link', { name: /create offer/i }).click();
    await expect(page).toHaveURL(/\/OfferLetter\/Edit\/\d+/);
  });

  await test.step('Reject an offer whose expiry is after the start date', async () => {
    await page.getByLabel('Salary', { exact: true }).fill('200000');
    await page.getByLabel('Start date').fill(isoDate(30));
    await page.getByLabel('Offer valid until').fill(isoDate(45));
    await page.getByRole('button', { name: /issue offer/i }).click();
    await expect(page.getByText('The offer expiry date must be on or before the start date.')).toBeVisible();
  });

  await test.step('Issue and view a valid offer', async () => {
    await page.getByLabel('Offer valid until').fill(isoDate(14));
    await page.getByRole('button', { name: /issue offer/i }).click();
    await expect(page).toHaveURL(/\/OfferLetter\/Details\/\d+/);
    await expect(page.getByRole('heading', { name: `Offer of Employment — ${jobTitle}` })).toBeVisible();
    await expect(page.getByText(candidateName, { exact: true })).toBeVisible();
    await expect(page.getByText(/LKR\s+200,000\.00\s+per month/)).toBeVisible();
    await expect(page.getByRole('button', { name: /print \/ save pdf/i })).toBeVisible();

    await page.goto('/OfferLetter/Index');
    const row = page.getByRole('row').filter({ hasText: email });
    await expect(row).toContainText('Issued');
    await expect(row.getByRole('link', { name: 'View' })).toBeVisible();
  });
});
