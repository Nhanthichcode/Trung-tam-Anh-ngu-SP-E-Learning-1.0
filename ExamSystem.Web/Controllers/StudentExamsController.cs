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
            // userAnswers: Key = QuestionId, Value = Đáp án chọn (A, B, C, D hoặc Text)

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
                UserId = User.Identity.Name, // Hoặc lấy ID từ Claims
                StartTime = DateTime.Now
            };

            foreach (var q in originalQuestions)
            {
                string selectedVal = userAnswers.ContainsKey(q.Id) ? userAnswers[q.Id] : null;
                bool isCorrect = false;

                // --- LOGIC CHẤM ĐIỂM MỚI (Ưu tiên bảng Answers) ---
                if (q.Answers != null && q.Answers.Any())
                {
                    // Tìm đáp án đúng trong DB
                    var dbCorrectAnswer = q.Answers.FirstOrDefault(a => a.IsCorrect);

                    // So sánh nội dung đáp án người dùng chọn với nội dung đáp án đúng
                    if (dbCorrectAnswer != null && selectedVal == dbCorrectAnswer.Content)
                    {
                        isCorrect = true;
                    }
                }
                // --- LOGIC FALLBACK (Dành cho câu hỏi cũ chưa migrate) ---
                else
                {
                    if (!string.IsNullOrEmpty(q.CorrectAnswer) &&
                        string.Equals(selectedVal, q.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        isCorrect = true;
                    }
                }

                if (isCorrect) correctCount++;

                // Lưu chi tiết từng câu
                //resultDetails.Add(new TestResult
                //{
                //    QuestionId = q.Id,
                //    TestAttemptId = int.Parse(selectedVal),
                //    IsCorrect = isCorrect
                //});
            }

            // Tính điểm
            attempt.Score = (double)correctCount / originalQuestions.Count * 10;
            attempt.TestResults = resultDetails;

            _context.TestAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction("Result", new { id = attempt.Id });
        }

        // 4. KẾT QUẢ (Giữ nguyên hoặc tùy chỉnh)
        public async Task<IActionResult> Result(int id)
        {
            var attempt = await _context.TestAttempts
                .Include(a => a.Exam)
                .Include(a => a.TestResults)
                .FirstOrDefaultAsync(a => a.Id == id);
            return View(attempt);
        }
    }
}