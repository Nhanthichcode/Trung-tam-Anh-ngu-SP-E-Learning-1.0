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

            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<Question>().ToTable("Questions");

            // --- CẤU HÌNH CASCADE DELETE CHO BÀI ĐỌC ---
            builder.Entity<Question>()
                .HasOne(q => q.ReadingPassage)
                .WithMany() // Nếu bạn không có Navigation Property trong ReadingPassage
                .HasForeignKey(q => q.ReadingPassageId)
                .OnDelete(DeleteBehavior.Cascade); // <<< THÊM: Khi xóa Passage, Questions liên quan sẽ bị xóa

            // --- CẤU HÌNH CASCADE DELETE CHO BÀI NGHE ---
            builder.Entity<Question>()
                .HasOne(q => q.ListeningResource)
                .WithMany() // Nếu bạn không có Navigation Property trong ListeningResource
                .HasForeignKey(q => q.ListeningResourceId)
                .OnDelete(DeleteBehavior.Cascade); // <<< THÊM: Khi xóa Resource, Questions liên quan sẽ bị xóa

            // --- CẤU HÌNH KHÓA CHÍNH KÉP (QUESTIONTOPIC) ---
            builder.Entity<QuestionTopic>()
                .HasKey(qt => new { qt.QuestionId, qt.TopicId });

            // Cấu hình xóa chuỗi cho QuestionTopic -> Question
            builder.Entity<QuestionTopic>()
                .HasOne(qt => qt.Question)
                .WithMany(q => q.QuestionTopics)
                .HasForeignKey(qt => qt.QuestionId)
                .OnDelete(DeleteBehavior.Cascade); // <<< THÊM: Khi xóa Question, các QuestionTopic liên quan sẽ bị xóa

            // Cấu hình xóa chuỗi cho QuestionTopic -> Topic
            builder.Entity<QuestionTopic>()
                .HasOne(qt => qt.Topic)
                .WithMany(t => t.QuestionTopics)
                .HasForeignKey(qt => qt.TopicId)
                .OnDelete(DeleteBehavior.Cascade); // <<< THÊM: Khi xóa Topic, các QuestionTopic liên quan sẽ bị xóa

            // --- CẤU HÌNH CASCADE DELETE CHO ANSWERS ---
            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade); // <<< THÊM: Khi xóa Question, Answers sẽ bị xóa
        }

    }
}
