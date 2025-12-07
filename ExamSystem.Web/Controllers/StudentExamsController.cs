using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ExamSystem.Web.Controllers
{
   // [Authorize(Roles = "Student")]
    public class StudentExamsController : Controller
    {
        private readonly AppDbContext _context;

        public StudentExamsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐỀ THI (Giữ nguyên)
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams.ToListAsync();
            return View(exams);
        }

        // 2. LÀM BÀI THI (GET) - Load dữ liệu từ bảng mới
        public async Task<IActionResult> TakeExam(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Load câu hỏi kèm theo Answers, ReadingPassage, ListeningResource
            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.Answers)          // Lấy đáp án
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.ReadingPassage)   // Lấy bài đọc
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.ListeningResource)// Lấy file nghe
                .Where(eq => eq.ExamId == id)
                .OrderBy(eq => eq.QuestionId) // Hoặc order theo thứ tự đề
                .ToListAsync();

            ViewBag.ExamTitle = exam.Title;
            ViewBag.ExamId = exam.Id;
            ViewBag.Duration = exam.DurationMinutes;

            // Trả về danh sách câu hỏi để View xử lý
            var questions = examQuestions.Select(eq => eq.Question).ToList();
            return View(questions);
        }
        

        // 3. NỘP BÀI (POST) - Chấm điểm theo cấu trúc mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(int examId, Dictionary<int, string> userAnswers)
        {
            
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra xem đã đăng nhập chưa
            if (currentUserId == null)
            {
                // Đây là trường hợp khẩn cấp, nên chuyển hướng đến trang đăng nhập hoặc báo lỗi
                return Challenge();
            }

            // Lấy lại bộ câu hỏi đề thi để so sánh đáp án
            var originalQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.Answers) // Cần bảng Answers để chấm điểm
                .Where(eq => eq.ExamId == examId)
                .Select(eq => eq.Question)
                .ToListAsync();

            int correctCount = 0;
            var resultDetails = new List<TestResult>();

            // Tạo lượt thi mới
            var attempt = new TestAttempt
            {
                ExamId = examId,
                UserId = currentUserId, // <<< SỬA TỪ USER.IDENTITY.NAME SANG CURRENTUSERID (ID DB)
                StartTime = DateTime.Now
            };

            foreach (var q in originalQuestions)
            {
                string selectedVal = userAnswers.ContainsKey(q.Id) ? userAnswers[q.Id] : null;
                bool isCorrect = false;

                // --- LOGIC CHẤM ĐIỂM ---
                if (q.Answers != null && q.Answers.Any())
                {
                    var dbCorrectAnswer = q.Answers.FirstOrDefault(a => a.IsCorrect);
                    if (dbCorrectAnswer != null && selectedVal == dbCorrectAnswer.Content)
                    {
                        isCorrect = true;
                    }
                }
                else // Fallback
                {
                    if (!string.IsNullOrEmpty(q.CorrectAnswer) && string.Equals(selectedVal, q.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        isCorrect = true;
                    }
                }

                if (isCorrect) correctCount++;

                resultDetails.Add(new TestResult
                {
                    QuestionId = q.Id,
                    SelectedAnswer = selectedVal, // Lưu nội dung đáp án sinh viên chọn
                    IsCorrect = isCorrect
                });
            }

            // Tính điểm
            attempt.Score = originalQuestions.Count > 0 ? (double)correctCount / originalQuestions.Count * 10 : 0;
            attempt.TestResults = resultDetails;

            _context.TestAttempts.Add(attempt);
            await _context.SaveChangesAsync(); // Lỗi Foreign Key sẽ không còn xảy ra ở đây

            return RedirectToAction("Result", new { id = attempt.Id });
        }

        public async Task<IActionResult> Result(int id)
        {
            var attempt = await _context.TestAttempts
                .Include(a => a.Exam) // Tải thông tin đề thi
                .Include(a => a.TestResults) // Tải chi tiết kết quả
                    .ThenInclude(tr => tr.Question) // <<< FIX QUAN TRỌNG: Tải thông tin Câu hỏi cho từng TestResult
                .FirstOrDefaultAsync(a => a.Id == id);

            // Thêm các ThenInclude cần thiết để hiển thị (MediaUrl, Answers, etc.)
            // Nếu bạn cần hiển thị đáp án đúng (từ Answers) và link Media:
            if (attempt?.TestResults != null)
            {
                // Để tránh code quá dài, ta dùng Select và Load lại dữ liệu cần thiết
                var questionIds = attempt.TestResults.Select(tr => tr.QuestionId).ToList();

                var detailedQuestions = await _context.Questions
                    .Include(q => q.Answers) // Cần để hiển thị đáp án đúng A, B, C, D
                    .Where(q => questionIds.Contains(q.Id))
                    .ToListAsync();

                // Gán lại Question đã có đủ Include (Đây là giải pháp an toàn nhất)
                foreach (var tr in attempt.TestResults)
                {
                    tr.Question = detailedQuestions.FirstOrDefault(q => q.Id == tr.QuestionId);
                }
            }

            if (attempt == null) return NotFound();

            return View(attempt);
        }
    }
}