using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
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
            // 1. Kiểm tra nếu đã có Exam thì không nạp thêm (tránh trùng lặp)
            if (await context.Exams.AnyAsync()) return;

            // =========================================================================
            // A. TẠO TÀI NGUYÊN (READING PASSAGES & LISTENING RESOURCES)
            // =========================================================================

            // A1. Bài đọc
            var readingPassage = new ReadingPassage
            {
                Title = "Benefits of Early Rising",
                Content = "Early rising is a good habit. It leads to health and happiness. The man who rises late can have little rest in the course of the day..."
            };
            context.ReadingPassages.Add(readingPassage);

            // A2. Bài nghe
            var listeningResource = new ListeningResource
            {
                Title = "Daily Routine Conversation",
                AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3", // Link mẫu
                Transcript = "John: What time do you usually wake up?\nSarah: I wake up at 6 AM every day to go jogging."
            };
            context.ListeningResources.Add(listeningResource);

            await context.SaveChangesAsync(); // Lưu để lấy ID dùng cho bước sau

            // =========================================================================
            // B. TẠO CÂU HỎI & ĐÁP ÁN (CHO ĐỦ 5 KỸ NĂNG)
            // =========================================================================
            var questions = new List<Question>();

            // 1. Kỹ năng GRAMMAR (Độc lập - Trắc nghiệm)
            var qGrammar = new Question
            {
                Content = "She ______ to the market yesterday.",
                SkillType = Core.Enums.ExamSkill.Grammar,
                Level = 1,
                CreatedDate = DateTime.Now,
                Answers = new List<Answer>
        {
            new Answer { Content = "go", IsCorrect = false },
            new Answer { Content = "went", IsCorrect = true },
            new Answer { Content = "gone", IsCorrect = false },
            new Answer { Content = "goes", IsCorrect = false }
        }
            };
            questions.Add(qGrammar);

            // 2. Kỹ năng READING (Gắn với bài đọc A1 - Trắc nghiệm)
            var qReading = new Question
            {
                Content = "According to the passage, what does early rising lead to?",
                SkillType = Core.Enums.ExamSkill.Reading,
                Level = 1,
                ReadingPassageId = readingPassage.Id, // Link ngoại
                CreatedDate = DateTime.Now,
                Answers = new List<Answer>
        {
            new Answer { Content = "Health and happiness", IsCorrect = true },
            new Answer { Content = "Wealth and power", IsCorrect = false },
            new Answer { Content = "Stress and anxiety", IsCorrect = false },
            new Answer { Content = "Nothing special", IsCorrect = false }
        }
            };
            questions.Add(qReading);

            // 3. Kỹ năng LISTENING (Gắn với bài nghe A2 - Trắc nghiệm)
            var qListening = new Question
            {
                Content = "What time does Sarah wake up?",
                SkillType = Core.Enums.ExamSkill.Listening,
                Level = 2,
                ListeningResourceId = listeningResource.Id, // Link ngoại
                CreatedDate = DateTime.Now,
                Answers = new List<Answer>
        {
            new Answer { Content = "5 AM", IsCorrect = false },
            new Answer { Content = "6 AM", IsCorrect = true },
            new Answer { Content = "7 AM", IsCorrect = false },
            new Answer { Content = "8 AM", IsCorrect = false }
        }
            };
            questions.Add(qListening);

            // 4. Kỹ năng SPEAKING (Độc lập - Có hình ảnh - Tự luận/Ghi âm)
            var qSpeaking = new Question
            {
                Content = "Describe the picture below and talk about your favorite daily activity.",
                SkillType = Core.Enums.ExamSkill.Speaking,
                Level = 3,
                MediaUrl = "https://picsum.photos/300/200", // Ảnh mẫu random
                CreatedDate = DateTime.Now,
                // Speaking thường không có đáp án trắc nghiệm, nhưng ta có thể để list rỗng hoặc null tùy logic
                Answers = new List<Answer>()
            };
            questions.Add(qSpeaking);

            // 5. Kỹ năng WRITING (Độc lập - Tự luận)
            var qWriting = new Question
            {
                Content = "Write an essay (approx 200 words) about the advantages of public transport.",
                SkillType = Core.Enums.ExamSkill.Writing,
                Level = 3,
                CreatedDate = DateTime.Now,
                Answers = new List<Answer>()
            };
            questions.Add(qWriting);

            context.Questions.AddRange(questions);
            await context.SaveChangesAsync();

            // =========================================================================
            // C. TẠO ĐỀ THI (EXAM) & CẤU TRÚC (EXAM PARTS)
            // =========================================================================

            var exam = new Exam
            {
                Title = "Mock Test Full Skills (A1-B1)",
                Description = "Đề thi thử tổng hợp bao gồm Ngữ pháp, Đọc, Nghe, Nói và Viết.",
                DurationMinutes = 60,
                StartDate = DateTime.Now,
                IsActive = true,
            };
            context.Exams.Add(exam);
            await context.SaveChangesAsync();

            // Tạo 3 phần thi (Parts)
            var part1 = new ExamPart { ExamId = exam.Id, Name = "Part 1: Grammar & Reading", OrderIndex = 1 };
            var part2 = new ExamPart { ExamId = exam.Id, Name = "Part 2: Listening", OrderIndex = 2 };
            var part3 = new ExamPart { ExamId = exam.Id, Name = "Part 3: Speaking & Writing", OrderIndex = 3 };

            context.ExamParts.AddRange(part1, part2, part3);
            await context.SaveChangesAsync();

            // =========================================================================
            // D. GÁN CÂU HỎI VÀO ĐỀ THI (EXAM QUESTIONS)
            // =========================================================================

            var examQuestions = new List<ExamQuestion>
    {
        // Part 1 chứa Grammar & Reading
        new ExamQuestion { ExamPartId = part1.Id, QuestionId = qGrammar.Id, Score = 10, SortOrder = 1 },
        new ExamQuestion { ExamPartId = part1.Id, QuestionId = qReading.Id, Score = 20, SortOrder = 2 },

        // Part 2 chứa Listening
        new ExamQuestion { ExamPartId = part2.Id, QuestionId = qListening.Id, Score = 20, SortOrder = 1 },

        // Part 3 chứa Speaking & Writing
        new ExamQuestion { ExamPartId = part3.Id, QuestionId = qSpeaking.Id, Score = 25, SortOrder = 1 },
        new ExamQuestion { ExamPartId = part3.Id, QuestionId = qWriting.Id, Score = 25, SortOrder = 2 }
    };

            context.ExamQuestions.AddRange(examQuestions);
            await context.SaveChangesAsync();

            // =========================================================================
            // E. TẠO DỮ LIỆU GIẢ LẬP KẾT QUẢ THI (TEST ATTEMPT & RESULT)
            // (Để test trang Lịch sử thi / Chấm điểm)
            // =========================================================================

            // Giả sử có một User ID (nếu chưa có user thì để null hoặc chuỗi GUID giả)
            var userId = "test-user-01";

            var attempt = new TestAttempt
            {
                ExamId = exam.Id,
                UserId = userId,
                StartTime = DateTime.Now.AddHours(-2),
                SubmitTime = DateTime.Now.AddHours(-1),
            };
            context.TestAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var vstep = new ExamStructure
            {
                Name = "VSTEP (Tiêu chuẩn)",
                Description = "Cấu trúc 4 kỹ năng: Nghe (4 parts), Đọc (4 parts), Viết (2 parts), Nói (2 parts)"
            };
            context.ExamStructures.Add(vstep);
            await context.SaveChangesAsync();

            var vstepParts = new List<StructurePart>();
            int order = 1;

            // --- KỸ NĂNG NGHE (LISTENING) - 4 PARTS ---
            // (Lưu ý: VSTEP 3-5 chuẩn thường là 3 part, nhưng mình làm 4 theo yêu cầu của bạn)
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 1: Hướng dẫn & Ví dụ", OrderIndex = order++, Description = "Nghe thông báo, hướng dẫn ngắn", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 2: Hội thoại", OrderIndex = order++, Description = "Nghe hội thoại và trả lời câu hỏi", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 3: Bài nói chuyện/Bài giảng", OrderIndex = order++, Description = "Nghe bài giảng dài", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 4: Phỏng vấn/Thảo luận", OrderIndex = order++, Description = "Nghe phỏng vấn phức tạp", SkillType = ExamSkill.Listening });

            // --- KỸ NĂNG ĐỌC (READING) - 4 PARTS ---
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 1: Từ vựng & Ngữ pháp", OrderIndex = order++, Description = "Điền từ vào chỗ trống", SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 2: Đọc biển báo/Thông báo", OrderIndex = order++, Description = "Hiểu ý chính thông báo",SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 3: Đọc hiểu văn bản", OrderIndex = order++, Description = "Đọc đoạn văn và trả lời", SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 4: Đọc hiểu nâng cao", OrderIndex = order++, Description = "Đọc bài báo/tạp chí chuyên sâu", SkillType = ExamSkill.Reading });

            // --- KỸ NĂNG VIẾT (WRITING) - 2 PARTS ---
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[WRITING] Part 1: Viết thư/Email", OrderIndex = order++, Description = "Viết một bức thư khoảng 120 từ", SkillType = ExamSkill.Writing });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[WRITING] Part 2: Viết luận (Essay)", OrderIndex = order++, Description = "Viết bài luận khoảng 250 từ", SkillType = ExamSkill.Writing });

            // --- KỸ NĂNG NÓI (SPEAKING) - 2 PARTS ---
            // (VSTEP chuẩn là 3 part, mình làm 2 theo yêu cầu)
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[SPEAKING] Part 1: Tương tác xã hội", OrderIndex = order++, Description = "Trả lời câu hỏi về bản thân", SkillType = ExamSkill.Speaking });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[SPEAKING] Part 2: Thảo luận giải pháp/Phát triển chủ đề", OrderIndex = order++, Description = "Thảo luận và đưa ra ý kiến", SkillType = ExamSkill.Speaking });

            context.StructureParts.AddRange(vstepParts);
     
            await context.SaveChangesAsync();
        }
    }
}