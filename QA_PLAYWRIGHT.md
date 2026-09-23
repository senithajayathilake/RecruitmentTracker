# Playwright QA tests

The suite checks authentication, HR access control, and the full path from creating a vacancy and candidate to issuing an offer letter. The hiring test checks that the candidate's Hired button updates an existing application and that an offer with an expiry date after the start date is rejected.

## Run on Windows

Install the .NET 8 SDK, Node.js 20 or newer, and SQL Server LocalDB (included with many Visual Studio installations). From the folder containing `RecruitmentTracker.sln`, run:

```powershell
npm install
npx playwright install chromium
npm run test:e2e
```

The tests start the ASP.NET app on `http://127.0.0.1:64563`. Stop a Visual Studio instance using that port first. The app uses a separate LocalDB database named `RecruitmentTracker_Playwright`; offer JSON files go into `.playwright/offer-letters`. The demo HR and candidate accounts are seeded automatically. The tests add unique QA candidates and vacancies to that test database.

The isolated offer directory requires the `OfferFolder` property in `Controllers/OfferLetterController.cs` to read the `OFFER_LETTER_DIRECTORY` environment variable. This is already changed in the supplied local project. If copying just the QA files to another checkout, use:

```csharp
private string OfferFolder =>
    Environment.GetEnvironmentVariable("OFFER_LETTER_DIRECTORY") is { Length: > 0 } folder
        ? Path.GetFullPath(folder)
        : Path.Combine(_environment.ContentRootPath, "App_Data", "OfferLetters");
```

Add `.playwright/`, `playwright-report/`, and `test-results/` to `.gitignore` so generated offers and browser traces stay out of Git.

To watch the browser, run `npm run test:e2e:headed`. After a run, use `npm run test:e2e:report` for the HTML report. Screenshots and traces for failures are kept in `test-results`.

To use a different SQL Server test database, set `PLAYWRIGHT_DB_CONNECTION` in the terminal before running the tests. To test an already running app instead, set `PLAYWRIGHT_EXTERNAL_SERVER=1` and `PLAYWRIGHT_BASE_URL` to its URL; that mode writes test records to the database used by that app.

The same tests run in GitHub Actions on a Windows runner when you push to `master` or `main`, open a pull request, or start the workflow manually. A failed run uploads the HTML report and trace files as an artifact.

These tests expect the `HireForVacancy` candidate action and Offer Letters feature from the current local project. The public GitHub page was unavailable during setup, so compare your pushed files with the local project before running the suite against a fresh clone.
