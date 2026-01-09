using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamSystem.Web.Areas.Student.Controllers
{
    [Area("Student")] // 2. Thêm Attribute này
    // [Authorize(Roles = "Student")]
    public class StudentExamsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // 1. Inject môi trường
        private readonly UserManager<AppUser> _userManager;
        public StudentExamsController(
        AppDbContext context,
        IWebHostEnvironment webHostEnvironment,
        UserManager<AppUser> userManager) // <--- Thêm tham số này
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager; // <--- Gán giá trị
        }

        // GET: /Test/Index
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách đề thi (Có thể thêm điều kiện .Where(e => e.IsPublished) nếu có)
            var exams = await _context.Exams
                .Include(e => e.ExamParts) // Include để đếm số phần thi
               .Where(e => e.IsActive == true)
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            return View(exams);
        }
        public async Task<IActionResult> Take(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamParts)
                    .ThenInclude(ep => ep.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Answers)
                .Include(e => e.ExamParts)
                    .ThenInclude(ep => ep.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.ReadingPassage)
                .Include(e => e.ExamParts)
                    .ThenInclude(ep => ep.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.ListeningResource)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound();

            // Sắp xếp các Part theo OrderIndex để đảm bảo Part 1, Part 2, Part 3 hiện đúng thứ tự
            exam.ExamParts = exam.ExamParts.OrderBy(p => p.OrderIndex).ToList();

            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int examId,
            Dictionary<int, int> answers,
            Dictionary<int, string> essayAnswers)
        // Lưu ý: Tôi đã bỏ tham số audioAnswers ở đây để tự xử lý bên dưới cho chắc chắn
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            // 1. Khởi tạo các Dictionary để tránh Null Reference
            answers = answers ?? new Dictionary<int, int>();
            essayAnswers = essayAnswers ?? new Dictionary<int, string>();

            // 2. Lấy đề thi
            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question).ThenInclude(q => q.Answers)
                .Where(eq => eq.ExamPart.ExamId == examId)
                .AsNoTracking().ToListAsync();

            if (!examQuestions.Any()) return NotFound("Lỗi: Không tìm thấy đề thi.");

            // 3. Tạo lượt thi
            var attempt = new TestAttempt
            {
                UserId = userId,
                ExamId = examId,
                StartTime = DateTime.Now.AddMinutes(-60),
                SubmitTime = DateTime.Now,
                Status = (int)TestStatus.Graded
            };

            var resultsList = new List<TestResult>();
            double totalScore = 0;
            bool hasManualGrading = false;

            // 4. CHUẨN BỊ THƯ MỤC UPLOAD
            // Chỉ tạo khi có file thực sự, nhưng ta cứ lấy đường dẫn trước
            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "student_exams", userId, $"{examId}_{DateTime.Now.Ticks}");

            // ==========================================================================================
            // [FIX QUAN TRỌNG]: LẤY FILE THỦ CÔNG TỪ REQUEST
            // ==========================================================================================
            var uploadedFiles = Request.Form.Files; // Lấy toàn bộ file được gửi lên
            if (uploadedFiles.Count > 0)
            {
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            }

            foreach (var eq in examQuestions)
            {
                var result = new TestResult { QuestionId = eq.QuestionId };

                // --- A. TRẮC NGHIỆM ---
                if (answers.ContainsKey(eq.QuestionId))
                {
                    int studentAnsId = answers[eq.QuestionId];
                    result.SelectedAnswerId = studentAnsId;
                    var correctAnswer = eq.Question.Answers.FirstOrDefault(a => a.IsCorrect == true);
                    if (correctAnswer != null && correctAnswer.Id == studentAnsId)
                    {
                        result.IsCorrect = true; result.ScoreObtained = eq.Score; totalScore += eq.Score;
                    }
                    else
                    {
                        result.IsCorrect = false; result.ScoreObtained = 0;
                    }
                }
                // --- B. TỰ LUẬN (WRITING) ---
                else if (essayAnswers.ContainsKey(eq.QuestionId))
                {
                    result.TextAnswer = essayAnswers[eq.QuestionId];
                    result.IsCorrect = null;
                    hasManualGrading = true;
                }
                // --- C. GHI ÂM (SPEAKING) - XỬ LÝ MỚI ---
                else if (eq.Question.SkillType == ExamSystem.Core.Enums.ExamSkill.Speaking)
                {
                    // Tìm file trong Request có name khớp với "audioAnswers[QuestionId]"
                    // Name trong HTML là: audioAnswers[105] -> Ta tìm file nào có Name chứa [105]
                    var file = uploadedFiles.FirstOrDefault(f => f.Name == $"audioAnswers[{eq.QuestionId}]");

                    if (file != null && file.Length > 0)
                    {
                        // Tạo tên file
                        string uniqueFileName = $"q_{eq.QuestionId}_{Guid.NewGuid().ToString().Substring(0, 8)}.webm"; // Webm vì Chrome ghi format này
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);

                        // Lưu file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        // Lưu DB
                        string relativePath = Path.Combine("uploads", "student_exams", userId, Path.GetFileName(uploadFolder), uniqueFileName).Replace("\\", "/");
                        result.AudioAnswerUrl = "/" + relativePath;
                        result.IsCorrect = null;
                        hasManualGrading = true;
                    }
                    else
                    {
                        // Không có file hoặc file rỗng
                        result.IsCorrect = false;
                    }
                }
                else
                {
                    result.IsCorrect = false;
                }

                resultsList.Add(result);
            }

            attempt.Score = totalScore;
            attempt.TestResults = resultsList;

            if (hasManualGrading)
            {
                attempt.Status = (int)TestStatus.Submitted;
                attempt.TeacherFeedback = "Đang chờ chấm điểm phần Viết/Nói.";
            }

            _context.TestAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction("Result", new { attemptId = attempt.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int attemptId)
        {
            var attempt = await _context.TestAttempts
         // 1. Load thông tin Đề thi và Cấu trúc đề (Parts -> Questions)
         .Include(ta => ta.Exam)
             .ThenInclude(e => e.ExamParts)
                 .ThenInclude(ep => ep.ExamQuestions)

         // 2. Load thông tin User
         .Include(ta => ta.User)

         // 3. Load Kết quả làm bài
         .Include(ta => ta.TestResults)
             .ThenInclude(tr => tr.Question)
                 .ThenInclude(q => q.Answers)
         .Include(ta => ta.TestResults)
             .ThenInclude(tr => tr.Question)
                 .ThenInclude(q => q.ReadingPassage)
         .Include(ta => ta.TestResults)
             .ThenInclude(tr => tr.Question)
                 .ThenInclude(q => q.ListeningResource)
         .FirstOrDefaultAsync(ta => ta.Id == attemptId);

            if (attempt == null) return NotFound();

            attempt.Exam.ExamParts = attempt.Exam.ExamParts.OrderBy(p => p.OrderIndex).ToList();

            return View(attempt);
        }

        // GET: /Test/History
        public async Task<IActionResult> History()
        {
            // 1. Lấy ID người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var userId = "User_01"; // Mock tạm, nhớ thay bằng User thật khi chạy Identity

            // 2. Lấy danh sách các lần thi của user này
            var attempts = await _context.TestAttempts
                .Include(ta => ta.Exam) // Include để lấy tên đề thi
                .Where(ta => ta.UserId == userId)
                .OrderByDescending(ta => ta.StartTime) // Mới nhất lên đầu
                .ToListAsync();

            return View(attempts);
        }
    }
}