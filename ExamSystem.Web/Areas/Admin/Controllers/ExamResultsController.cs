using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamResultsController : Controller
    {
        private readonly AppDbContext _context;

        public ExamResultsController(AppDbContext context)
        {
            _context = context;
        }

        // Xem danh sách tất cả các lượt thi của thí sinh
        public async Task<IActionResult> Index()
        {
            var attempts = await _context.TestAttempts
                .Include(ta => ta.Exam)
                .Include(ta => ta.User)
                .OrderByDescending(ta => ta.StartTime)
                .ToListAsync();
            return View(attempts);
        }

        // Trang chi tiết để giáo viên xem file và chấm điểm
        public async Task<IActionResult> Grade(int id)
        {
            var attempt = await _context.TestAttempts
                .Include(ta => ta.Exam)
                .Include(ta => ta.User)
                .Include(ta => ta.TestResults)
                    .ThenInclude(tr => tr.Question)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (attempt == null) return NotFound();
            return View(attempt);
        }

        // Action xử lý lưu điểm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrades(int attemptId, Dictionary<int, double> scores, Dictionary<int, string> notes)
        {
            // Load cả câu hỏi để check loại câu hỏi (QuestionType)
            var results = await _context.TestResults
                .Include(tr => tr.Question)
                .Where(tr => tr.TestAttemptId == attemptId)
                .ToListAsync();

            foreach (var result in results)
            {
                // CHỈ CẬP NHẬT ĐIỂM CHO CÂU TỰ LUẬN (WRITING / SPEAKING)
                // Bỏ qua câu MultipleChoice (Trắc nghiệm) để bảo toàn điểm máy chấm
                if (result.Question.QuestionType != ExamSystem.Core.Enums.QuestionType.MultipleChoice)
                {
                    if (scores.ContainsKey(result.Id))
                    {
                        result.ScoreObtained = scores[result.Id]; // Lưu điểm giáo viên chấm
                        result.Feedback = notes.ContainsKey(result.Id) ? notes[result.Id] : "";
                    }
                }
                else
                {
                    // Với câu trắc nghiệm, nếu giáo viên có nhập Feedback thì vẫn lưu Feedback, nhưng KHÔNG sửa điểm
                    if (notes.ContainsKey(result.Id))
                    {
                        result.Feedback = notes[result.Id];
                    }
                }
            }

            // Cập nhật tổng điểm cho lượt thi (TestAttempt)
            var attempt = await _context.TestAttempts.FindAsync(attemptId);
            if (attempt != null)
            {
                // Tổng điểm mới = Tổng điểm tất cả các câu cộng lại
                attempt.Score = results.Sum(r => r.ScoreObtained ?? 0);

                // Cập nhật trạng thái: 2 = Graded (Đã chấm xong)
                attempt.Status = (int)ExamSystem.Core.Enums.TestStatus.Graded;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu kết quả chấm thi thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}