using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Vacancy> Vacancies => Set<Vacancy>();
    public DbSet<InterviewStage> InterviewStages => Set<InterviewStage>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateCv> CandidateCvs => Set<CandidateCv>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vacancy>()
            .HasOne(v => v.CreatedByUser)
            .WithMany()
            .HasForeignKey(v => v.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InterviewStage>()
            .HasOne(s => s.Vacancy)
            .WithMany(v => v.InterviewStages)
            .HasForeignKey(s => s.VacancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Candidate>()
            .HasOne(c => c.ApplicationUser)
            .WithMany()
            .HasForeignKey(c => c.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Candidate>()
            .HasIndex(c => c.ApplicationUserId)
            .IsUnique()
            .HasFilter("[ApplicationUserId] IS NOT NULL");

        builder.Entity<CandidateCv>()
            .HasOne(c => c.Candidate)
            .WithMany(c => c.Cvs)
            .HasForeignKey(c => c.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Application>()
            .HasOne(a => a.Candidate)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Application>()
            .HasOne(a => a.Vacancy)
            .WithMany(v => v.Applications)
            .HasForeignKey(a => a.VacancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Application>()
            .HasOne(a => a.CandidateCv)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateCvId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Application>()
            .HasOne(a => a.CurrentInterviewStage)
            .WithMany()
            .HasForeignKey(a => a.CurrentInterviewStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Interview>()
            .HasOne(i => i.Application)
            .WithMany(a => a.Interviews)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Interview>()
            .HasOne(i => i.InterviewStage)
            .WithMany()
            .HasForeignKey(i => i.InterviewStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Interview>()
            .HasOne(i => i.Interviewer)
            .WithMany()
            .HasForeignKey(i => i.InterviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InterviewFeedback>()
            .HasOne(f => f.Interview)
            .WithOne(i => i.Feedback)
            .HasForeignKey<InterviewFeedback>(f => f.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InterviewFeedback>()
            .HasOne(f => f.Interviewer)
            .WithMany()
            .HasForeignKey(f => f.InterviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Candidate>().Property(c => c.Status).HasMaxLength(40);
        builder.Entity<Vacancy>().Property(v => v.Status).HasMaxLength(30);
        builder.Entity<Application>().Property(a => a.Status).HasMaxLength(40);

        builder.Entity<Interview>()
            .HasIndex(i => new { i.ApplicationId, i.InterviewStageId, i.Status });

        builder.Entity<InterviewFeedback>()
            .HasIndex(f => f.InterviewId)
            .IsUnique();
    }
}
