using ExamSystem.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Khai báo các bảng dữ liệu của bạn
        public DbSet<Question> Questions { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<TestAttempt> TestAttempts { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<QuestionTopic> QuestionTopics { get; set; }
        public DbSet<ReadingPassage> ReadingPassages { get; set; }
        public DbSet<ListeningResource> ListeningResources { get; set; }
        public DbSet<Answer> Answers { get; set; }
        // Sau này có thêm bảng nào (như Exam, TestAttempt) thì thêm tiếp vào đây
        // public DbSet<Exam> Exams { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Tại đây bạn có thể cấu hình thêm (Fluent API)
            // Ví dụ: Đổi tên bảng User mặc định cho gọn
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<Question>().ToTable("Questions");
            builder.Entity<QuestionTopic>()
                .HasKey(qt => new { qt.QuestionId, qt.TopicId });

            builder.Entity<QuestionTopic>()
                .HasOne(qt => qt.Question)
                .WithMany(q => q.QuestionTopics)
                .HasForeignKey(qt => qt.QuestionId);

            builder.Entity<QuestionTopic>()
                .HasOne(qt => qt.Topic)
                .WithMany(t => t.QuestionTopics)
                .HasForeignKey(qt => qt.TopicId);
        }
    }
}
