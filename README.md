# RecruitmentTracker — Recruitment Workflow Update

This project is an ASP.NET Core MVC + SQL Server LocalDB recruitment tracker.

## Implemented features

### Role-based access
- Candidate
- HR
- HiringManager
- Interviewer

Normal registration automatically creates a Candidate role and a linked Candidate profile.

### Separate dashboards
- Candidate dashboard
- HR dashboard
- Hiring Manager dashboard
- Interviewer dashboard

### Candidate workflow
1. Register/login.
2. Candidate profile is created automatically.
3. Upload a PDF or DOCX CV.
4. Browse open vacancies.
5. Apply using the latest uploaded CV.
6. Track applications and AI match scores.

### HR workflow
- All registered candidates automatically appear on the Candidates page.
- View candidate profiles and CVs.
- View applications.
- Run AI CV screening.
- Shortlist/reject/move applications.
- Create and manage vacancies.
- Manage user role access by registered email.

### Hiring Manager workflow
- Separate Hiring Manager dashboard.
- Review applications and AI screening results.
- Make application status decisions.

### CV screening
The project includes a local, explainable AI-style screening engine. It compares:
- vacancy requirements
- vacancy description
- candidate skills
- qualification
- experience
- extracted CV text

It produces:
- match score
- matched terms
- missing terms
- summary
- recommendation

PDF text is extracted with PdfPig and DOCX text with Open XML.

## Database

The project uses:
`(localdb)\MSSQLLocalDB`

Database:
`RecruitmentTrackerDB`

For this development build, `EnsureCreatedAsync()` is used. If an older database is missing the new `Applications` table or `Candidates.ApplicationUserId`, the application recreates the development database once so the schema matches the project.

**The recreate step deletes the existing development database. Do not use this database initialization strategy for production data.**

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

## First run

1. Open the solution/project in Visual Studio 2022.
2. Ensure .NET 8 SDK and SQL Server LocalDB are installed.
3. Restore NuGet packages.
4. Build → Rebuild Solution.
5. Press F5.
6. The development database will be created/recreated as required.
7. Use the demo HR account to test role management, candidates, vacancies and AI screening.

## Important

The CV upload feature stores files under `wwwroot/uploads/cvs`. For production, use protected object/file storage instead of directly serving CV files from the public web root.
