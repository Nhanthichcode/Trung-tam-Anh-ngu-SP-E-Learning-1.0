using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;

namespace ExamSystem.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ListeningResource> ListeningResources { get; set; }
        public DbSet<ReadingPassage> ReadingPassages { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamPart> ExamParts { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<TestAttempt> TestAttempts { get; set; }
        public DbSet<TestResult> TestResults { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- CẤU HÌNH QUAN TRỌNG ĐỂ TRÁNH LỖI MIGRATION ---

            // 1. Cấu hình SetNull cho Resource (Câu hỏi không bị xóa khi Resource bị xóa)
            builder.Entity<Question>()
                .HasOne(q => q.ReadingPassage)
                .WithMany()
                .HasForeignKey(q => q.ReadingPassageId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Question>()
                .HasOne(q => q.ListeningResource)
                .WithMany()
                .HasForeignKey(q => q.ListeningResourceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // 2. Cấu hình Cascade Delete cho cấu trúc đề thi
            builder.Entity<ExamPart>()
                .HasOne(ep => ep.Exam)
                .WithMany(e => e.ExamParts)
                .HasForeignKey(ep => ep.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ExamQuestion>()
                .HasOne(eq => eq.ExamPart)
                .WithMany(ep => ep.ExamQuestions)
                .HasForeignKey(eq => eq.ExamPartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TestAttempt>()
                .HasOne(ta => ta.Exam)
                .WithMany()
                .HasForeignKey(ta => ta.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TestResult>()
                .HasOne(tr => tr.TestAttempt)
                .WithMany(ta => ta.TestResults)
                .HasForeignKey(tr => tr.TestAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}