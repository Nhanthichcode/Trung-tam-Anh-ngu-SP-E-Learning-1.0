using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;

namespace ExamSystem.Web.Controllers
{
    // Controller này chỉ dùng để chạy tool, chạy xong có thể xóa
    public class SystemMaintenanceController : Controller
    {
        private readonly AppDbContext _context;

        public SystemMaintenanceController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return Content("Chạy /SystemMaintenance/MigrateLegacyData để bắt đầu chuyển đổi dữ liệu.");
        }

        public async Task<IActionResult> MigrateLegacyData()
        {
            var questions = await _context.Questions
                                    .Include(q => q.Answers) // Load bảng con để check
                                    .ToListAsync();
            int countPassage = 0;
            int countAnswers = 0;

            foreach (var q in questions)
            {
                // 1. CHUYỂN ĐỔI BÀI ĐỌC (PassageText -> Table ReadingPassages)
                if (!string.IsNullOrEmpty(q.PassageText) && q.ReadingPassageId == null)
                {
                    // Kiểm tra xem đoạn văn này đã có trong bảng ReadingPassage chưa (tránh trùng lặp)
                    var existingPassage = await _context.ReadingPassages
                                                .FirstOrDefaultAsync(p => p.Content == q.PassageText);

                    if (existingPassage == null)
                    {
                        existingPassage = new ReadingPassage
                        {
                            Title = "Bài đọc tự động migrate " + DateTime.Now.Ticks,
                            Content = q.PassageText
                        };
                        _context.ReadingPassages.Add(existingPassage);
                        await _context.SaveChangesAsync(); // Lưu để lấy ID
                    }

                    q.ReadingPassageId = existingPassage.Id;
                    countPassage++;
                }

                // 2. CHUYỂN ĐỔI ĐÁP ÁN (OptionA... -> Table Answers)
                if (!string.IsNullOrEmpty(q.OptionA) && !q.Answers.Any())
                {
                    var options = new List<string> { q.OptionA, q.OptionB, q.OptionC, q.OptionD };
                    string correctChar = q.CorrectAnswer?.ToUpper() ?? "";

                    for (int i = 0; i < options.Count; i++)
                    {
                        if (string.IsNullOrEmpty(options[i])) continue;

                        char optionChar = (char)('A' + i); // 0->A, 1->B...
                        bool isCorrect = correctChar == optionChar.ToString();

                        var ans = new Answer
                        {
                            Content = options[i],
                            IsCorrect = isCorrect,
                            QuestionId = q.Id
                        };
                        _context.Answers.Add(ans);
                    }
                    countAnswers++;
                }
            }

            await _context.SaveChangesAsync();
            return Content($"Đã chuyển đổi thành công: {countPassage} bài đọc và {countAnswers} bộ đáp án.");
        }
    }
}