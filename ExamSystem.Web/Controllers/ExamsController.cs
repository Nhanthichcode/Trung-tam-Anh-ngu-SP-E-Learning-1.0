using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums; // Cần để dùng QuestionType
using ExamSystem.Infrastructure.Data;

namespace ExamSystem.Web.Controllers
{
    public class ExamsController : Controller
    {
        private readonly AppDbContext _context;

        public ExamsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Exams
        public async Task<IActionResult> Index()
        {
            return View(await _context.Exams.ToListAsync());
        }

        // GET: Exams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Load đầy đủ thông tin: Câu hỏi -> Bài đọc / File nghe đi kèm
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question).ThenInclude(q => q.ReadingPassage)
                .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question).ThenInclude(q => q.ListeningResource)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exam == null) return NotFound();

            return View(exam);
        }

        // GET: Exams/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,DurationMinutes,StartDate,EndDate,IsActive")] Exam exam)
        {
            if (ModelState.IsValid)
            {
                _context.Add(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // GET: Exams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            return View(exam);
        }

        // POST: Exams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DurationMinutes,StartDate,EndDate,IsActive")] Exam exam)
        {
            if (id != exam.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamExists(exam.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // GET: Exams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null) return NotFound();

            return View(exam);
        }

        // POST: Exams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null) _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // TÍNH NĂNG MỚI: QUẢN LÝ CÂU HỎI TRONG ĐỀ
        // =========================================================================

        // 1. TẠO ĐỀ TỰ ĐỘNG (RANDOM: 1 Đọc + 1 Nghe + 1 Nói + 1 Viết)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoGenerate(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // A. Random 1 Bài Đọc (Lấy tất cả câu hỏi thuộc bài đọc đó)
            var randomReading = await _context.ReadingPassages
                .Include(r => r.Questions)
                .OrderBy(r => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                .FirstOrDefaultAsync();

            // B. Random 1 Bài Nghe (Lấy tất cả câu hỏi thuộc bài nghe đó)
            var randomListening = await _context.ListeningResources
                .Include(l => l.Questions)
                .OrderBy(l => Guid.NewGuid())
                .FirstOrDefaultAsync();

            // C. Random 1 Câu Nói (Speaking)
            var randomSpeaking = await _context.Questions
                .Where(q => q.Type == QuestionType.Speaking)
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefaultAsync();

            // D. Random 1 Câu Viết (Writing)
            var randomWriting = await _context.Questions
                .Where(q => q.Type == QuestionType.Writing)
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefaultAsync();

            // E. Tổng hợp danh sách câu hỏi cần thêm
            var questionsToAdd = new List<Question>();
            if (randomReading != null) questionsToAdd.AddRange(randomReading.Questions);
            if (randomListening != null) questionsToAdd.AddRange(randomListening.Questions);
            if (randomSpeaking != null) questionsToAdd.Add(randomSpeaking);
            if (randomWriting != null) questionsToAdd.Add(randomWriting);

            // F. Lưu vào Database
            await AddQuestionsToExam(id, questionsToAdd);

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // 2. CHỌN CÂU HỎI THỦ CÔNG (Theo cấu trúc 4 phần) - GET
        [HttpGet]
        public async Task<IActionResult> AddQuestions(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            ViewData["ExamTitle"] = exam.Title;
            ViewData["ExamId"] = id;

            // Lấy dữ liệu đã phân loại để hiển thị lên View chọn
            ViewBag.ReadingPassages = await _context.ReadingPassages.Include(r => r.Questions).ToListAsync();
            ViewBag.ListeningResources = await _context.ListeningResources.Include(l => l.Questions).ToListAsync();
            ViewBag.SpeakingQuestions = await _context.Questions.Where(q => q.Type == QuestionType.Speaking).ToListAsync();
            ViewBag.WritingQuestions = await _context.Questions.Where(q => q.Type == QuestionType.Writing).ToListAsync();

            return View(); // Bạn sẽ cần cập nhật View AddQuestions.cshtml để hiển thị 4 danh sách này
        }

        // 3. XỬ LÝ LƯU CÂU HỎI THỦ CÔNG - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestionsManual(int examId,
            int? selectedReadingId,
            int? selectedListeningId,
            List<int> selectedSpeakingIds,
            List<int> selectedWritingIds)
        {
            var questionsToAdd = new List<Question>();

            // Lấy câu hỏi từ Bài Đọc được chọn
            if (selectedReadingId.HasValue)
            {
                var qs = await _context.Questions.Where(q => q.ReadingPassageId == selectedReadingId).ToListAsync();
                questionsToAdd.AddRange(qs);
            }

            // Lấy câu hỏi từ Bài Nghe được chọn
            if (selectedListeningId.HasValue)
            {
                var qs = await _context.Questions.Where(q => q.ListeningResourceId == selectedListeningId).ToListAsync();
                questionsToAdd.AddRange(qs);
            }

            // Lấy câu hỏi Speaking & Writing (Giảng viên có thể chọn nhiều câu)
            var otherIds = new List<int>();
            if (selectedSpeakingIds != null) otherIds.AddRange(selectedSpeakingIds);
            if (selectedWritingIds != null) otherIds.AddRange(selectedWritingIds);

            if (otherIds.Any())
            {
                var qs = await _context.Questions.Where(q => otherIds.Contains(q.Id)).ToListAsync();
                questionsToAdd.AddRange(qs);
            }

            await AddQuestionsToExam(examId, questionsToAdd);

            return RedirectToAction(nameof(Details), new { id = examId });
        }

        // --- Helper: Thêm danh sách câu hỏi vào đề (tránh trùng) ---
        private async Task AddQuestionsToExam(int examId, List<Question> questions)
        {
            if (questions == null || !questions.Any()) return;

            var existingIds = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .Select(eq => eq.QuestionId)
                .ToListAsync();

            foreach (var q in questions)
            {
                if (!existingIds.Contains(q.Id))
                {
                    _context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = examId,
                        QuestionId = q.Id,
                        Score = 1.0, // Điểm mặc định
                        SortOrder = 0
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
    }
}