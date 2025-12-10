using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.Infrastructure.Seeding
{
    public static class DbSeeder
    {
        public static async Task SeedAllAsync(IServiceProvider serviceProvider)
        {
            // Lấy các dịch vụ cần thiết từ ServiceProvider
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Seed Roles & Users (Identity)
            await SeedIdentityAsync(userManager, roleManager);

            // 2. Seed Data (Exams, Questions...)
            await SeedBusinessDataAsync(context);
        }

        private static async Task SeedIdentityAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // --- TẠO ROLE ---
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("Student"))
                await roleManager.CreateAsync(new IdentityRole("Student"));

            // --- TẠO ADMIN ---
            var adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên hệ thống",
                    EmailConfirmed = true,
                    DateOfBirth = new DateTime(1990, 1, 1),
                  //  IsActive = true // Giả sử bạn có trường này
                };
                // Password phải có: Hoa, thường, số, ký tự đặc biệt
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // --- TẠO STUDENT ---
            var studentEmail = "student@example.com";
            if (await userManager.FindByEmailAsync(studentEmail) == null)
            {
                var studentUser = new AppUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FullName = "Nguyễn Văn Sinh Viên",
                    EmailConfirmed = true,
                    DateOfBirth = new DateTime(2000, 5, 15),
                  //  IsActive = true
                };
                await userManager.CreateAsync(studentUser, "Student@123");
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }

        private static async Task SeedBusinessDataAsync(AppDbContext context)
        {
            // Kiểm tra nếu đã có dữ liệu thì không nạp thêm
            if (await context.Exams.AnyAsync()) return;

            // --- 1. TẠO TÀI NGUYÊN (READING/LISTENING) ---
            var readingPassage = new ReadingPassage
            {
                Title = "Lợi ích của việc học ngoại ngữ",
                Content = "Học ngoại ngữ không chỉ giúp bạn giao tiếp mà còn cải thiện trí nhớ..."
            };

            var listeningResource = new ListeningResource
            {
                Title = "Hội thoại tại sân bay",
                AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3", // Link mẫu
                Transcript = "A: Xin chào, tôi muốn làm thủ tục check-in.\nB: Vui lòng cho xem hộ chiếu."
            };

            context.ReadingPassages.Add(readingPassage);
            context.ListeningResources.Add(listeningResource);
            await context.SaveChangesAsync(); // Save để lấy ID

            // --- 2. TẠO CÂU HỎI & ĐÁP ÁN ---
            // Câu 1: Gắn với bài đọc
            var q1 = new Question
            {
                Content = "Theo bài đọc, lợi ích chính của học ngoại ngữ là gì?",
                ReadingPassageId = readingPassage.Id,
                SkillType = Core.Enums.ExamSkill.Reading, // 2 = Reading
                QuestionType = Core.Enums.QuestionType.MultipleChoice, // 1 = Multiple Choice
                Level = 1, // Dễ
                CreatedDate = DateTime.Now,
                Answers = new List<Answer>
                {
                    new Answer { Content = "Chỉ để đi du lịch", IsCorrect = false },
                    new Answer { Content = "Cải thiện trí nhớ và giao tiếp", IsCorrect = true },
                    new Answer { Content = "Kiếm nhiều tiền hơn", IsCorrect = false },
                    new Answer { Content = "Không có lợi ích gì", IsCorrect = false }
                }
            };

            // Câu 2: Gắn với bài nghe
            var q2 = new Question
            {
                Content = "Người nói A đang ở đâu?",
                ListeningResourceId = listeningResource.Id,
                SkillType = Core.Enums.ExamSkill.Listening, // 1 = Listening
                QuestionType = Core.Enums.QuestionType.Essay,
                Level = 1,
                CreatedDate = DateTime.Now,
                Answers = new List<Answer>
                {
                    new Answer { Content = "Tại nhà ga", IsCorrect = false },
                    new Answer { Content = "Tại sân bay", IsCorrect = true },
                    new Answer { Content = "Tại khách sạn", IsCorrect = false },
                    new Answer { Content = "Tại siêu thị", IsCorrect = false }
                }
            };

            context.Questions.AddRange(q1, q2);
            await context.SaveChangesAsync();

            // --- 3. TẠO ĐỀ THI & CẤU TRÚC ĐỀ ---
            var exam = new Exam
            {
                Title = "Đề thi thử Tiếng Anh A1",
                Description = "Đề thi kiểm tra kiến thức cơ bản",
                DurationMinutes = 45,
                StartDate = DateTime.Now,
                IsActive = true
            };
            context.Exams.Add(exam);
            await context.SaveChangesAsync();

            // Tạo phần thi (Part)
            var part1 = new ExamPart
            {
                Name = "Phần 1: Trắc nghiệm tổng hợp",
                OrderIndex = 1,
                ExamId = exam.Id
            };
            context.ExamParts.Add(part1);
            await context.SaveChangesAsync();

            // Gán câu hỏi vào phần thi (Bảng nối ExamQuestions)
            var eq1 = new ExamQuestion { ExamPartId = part1.Id, QuestionId = q1.Id, Score = 5, SortOrder = 1 };
            var eq2 = new ExamQuestion { ExamPartId = part1.Id, QuestionId = q2.Id, Score = 5, SortOrder = 2 };

            context.ExamQuestions.AddRange(eq1, eq2);
            await context.SaveChangesAsync();
        }
    }
}