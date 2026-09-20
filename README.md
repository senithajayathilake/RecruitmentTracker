# RecruitmentTracker — Final Recruitment & Hiring Tracker

This is the completed ASP.NET Core MVC prototype for the Recruitment & Hiring Tracker scenario.

## What is implemented

### Users and role-based access
- HR / Recruiter
- Candidate
- Interviewer
- Hiring Manager
- Role-specific dashboards and permissions
- HR can assign portal roles to registered accounts

### Vacancy management
- Create and edit positions
- Job description and requirements
- Department, location, employment type, salary and closing date
- Open / Closed / Draft status
- Configurable interview stages for every vacancy
- Optional feedback gate: the next stage can be blocked until feedback from the previous stage is submitted

### Candidate management
- Candidate registration automatically creates a candidate profile
- HR can also add candidates manually
- Profile information, skills, qualifications and experience
- PDF / DOCX CV upload
- Candidate can apply to open vacancies
- Duplicate application protection

### AI-style CV screening
- Local and explainable screening logic
- Uses candidate skills, experience, qualification and extracted CV text
- Compares them with the vacancy description and requirements
- Produces:
  - match score
  - matched skills
  - missing skills
  - summary
  - recommendation

This is a rules/heuristics-based local screening feature, not a hosted generative-AI service.

### Interview workflow
- HR schedules an interview for a specific application and vacancy stage
- HR assigns a registered Interviewer
- Date, time, duration, location / meeting link and notes
- Scheduled / Completed / Cancelled interview status
- Candidate and interviewer receive in-app notifications
- Interview stages remain configurable per vacancy

### Structured interviewer feedback
Every interviewer uses the same 1–5 rubric:
- Technical / role knowledge
- Communication
- Problem solving
- Culture / team fit
- Recommendation
- Evidence-based comments

This gives HR and management a consistent basis for comparing candidates.

### Candidate progression and decisions
Application states include:
- New
- Under Review
- Shortlisted
- Interview
- On Hold
- Hired
- Rejected

When the vacancy feedback gate is enabled, HR cannot schedule the next interview stage until feedback for the previous stage has been completed.

### Candidate comparison
HR and Hiring Managers can compare applicants for one vacancy in a single table using:
- experience
- skills
- AI CV match
- average interview score
- latest interviewer recommendation
- current stage
- current application status

### Management reporting
The Reports page includes:
- open vacancies
- total applications
- hired
- rejected
- on hold
- scheduled interviews
- completed interviews
- vacancy-level application and hiring figures
- average AI score by vacancy
- CSV export (opens in Excel)

### Dashboards
- HR: applications, candidates, interviews, comparison and reports
- Candidate: CV, applications, upcoming interviews and notifications
- Interviewer: assigned interviews, pending feedback and notifications
- Hiring Manager: applications, current stage, completed interviews, comparison and reports

## In-app notifications

The prototype includes in-app notifications for:
- interview scheduled
- interview cancelled
- feedback submitted
- application status changed
- new application received

External email/SMS sending is not configured because that requires a real mail/SMS provider and credentials. The scheduling and notification workflow is otherwise implemented in the application.

## Development database

Connection:
`(localdb)\MSSQLLocalDB`

Database:
`RecruitmentTrackerDB`

This prototype uses `EnsureCreatedAsync()` rather than Entity Framework migrations.

### Important first-run behaviour
If the existing local database does not contain the final interview/feedback/notification schema, the application automatically deletes and recreates `RecruitmentTrackerDB`.

That means **old local test data may be deleted on the first run of this final version**.

This is suitable for a university prototype but should not be used for production data.

## Demo accounts

HR:
- Email: `hr@recruitment.local`
- Password: `Hr12345`

Candidate:
- Email: `candidate@recruitment.local`
- Password: `Candidate123`

Interviewer:
- Email: `interviewer@recruitment.local`
- Password: `Interviewer123`

Hiring Manager:
- Email: `manager@recruitment.local`
- Password: `Manager123`

## Recommended demo flow

1. Log in as HR.
2. Create a vacancy.
3. Configure 2–3 interview stages for the vacancy.
4. Log in as Candidate and upload a CV.
5. Apply to the vacancy.
6. Log back in as HR and review the application.
7. Schedule the first interview and assign the demo interviewer.
8. Log in as Interviewer and submit structured feedback.
9. Log in as HR and schedule the next stage.
10. Use Compare Candidates to review evidence consistently.
11. Mark the application Hired / Rejected / On Hold.
12. Log in as Hiring Manager and open Reports.
13. Export the CSV report.

## First run

1. Open `RecruitmentTracker.sln` in Visual Studio 2022.
2. Confirm the .NET 8 SDK and SQL Server LocalDB are installed.
3. Restore NuGet packages if Visual Studio asks.
4. Build → Rebuild Solution.
5. Press F5.
6. The development database is created/recreated if required.
7. Start with the demo HR account.

## Security note

CV files are stored under `wwwroot/uploads/cvs` in this prototype. For a production system, CVs should be stored in protected/private storage and accessed through authorized endpoints.
