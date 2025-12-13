using ExamSystem.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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
        public DbSet<ExamStructure> ExamStructures { get; set; }
        public DbSet<StructurePart> StructureParts { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================================================================
            // 1. CẤU HÌNH QUAN HỆ CÂU HỎI VỚI BÀI ĐỌC / BÀI NGHE (QUAN TRỌNG)
            // =========================================================================

            // Mối quan hệ: Một Bài Đọc có Nhiều Câu Hỏi
            builder.Entity<Question>()
                .HasOne(q => q.ReadingPassage)          // Câu hỏi thuộc về 1 Bài đọc
                .WithMany(p => p.Questions)             // Bài đọc chứa danh sách các Câu hỏi (QUAN TRỌNG)
                .HasForeignKey(q => q.ReadingPassageId)
                .OnDelete(DeleteBehavior.SetNull);      // Xóa bài đọc -> FK câu hỏi thành null (không xóa câu hỏi)

            // Mối quan hệ: Một Bài Nghe có Nhiều Câu Hỏi
            builder.Entity<Question>()
                .HasOne(q => q.ListeningResource)       // Câu hỏi thuộc về 1 Bài nghe
                .WithMany(r => r.Questions)             // Bài nghe chứa danh sách các Câu hỏi (QUAN TRỌNG)
                .HasForeignKey(q => q.ListeningResourceId)
                .OnDelete(DeleteBehavior.SetNull);      // Xóa bài nghe -> FK câu hỏi thành null

            // =========================================================================
            // 2. CẤU HÌNH CASCADE DELETE (XÓA CHA MẤT CON) CHO CÁC BẢNG KHÁC
            // =========================================================================

            // Exam -> ExamParts
            builder.Entity<ExamPart>()
                .HasOne(ep => ep.Exam)
                .WithMany(e => e.ExamParts)
                .HasForeignKey(ep => ep.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamPart -> ExamQuestions (Câu hỏi trong đề thi)
            builder.Entity<ExamQuestion>()
                .HasOne(eq => eq.ExamPart)
                .WithMany(ep => ep.ExamQuestions)
                .HasForeignKey(eq => eq.ExamPartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question -> Answers (Câu hỏi -> Đáp án)
            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Exam -> TestAttempts (Lượt thi)
            builder.Entity<TestAttempt>()
                .HasOne(ta => ta.Exam)
                .WithMany()
                .HasForeignKey(ta => ta.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // TestAttempt -> TestResults (Kết quả chi tiết)
            builder.Entity<TestResult>()
                .HasOne(tr => tr.TestAttempt)
                .WithMany(ta => ta.TestResults)
                .HasForeignKey(tr => tr.TestAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            // Cấu hình cho Structure
            builder.Entity<StructurePart>()
                .HasOne(sp => sp.ExamStructure)
                .WithMany(es => es.Parts)
                .HasForeignKey(sp => sp.ExamStructureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}