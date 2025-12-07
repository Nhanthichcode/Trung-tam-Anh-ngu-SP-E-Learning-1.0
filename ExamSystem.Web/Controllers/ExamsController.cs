using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
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

        #region CRUD CƠ BẢN & INDEX (Giữ nguyên)
        public async Task<IActionResult> Index(string searchString, string filterStatus)
        {
            var examsQuery = _context.Exams.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                examsQuery = examsQuery.Where(e => e.Title.Contains(searchString) || (e.Description != null && e.Description.Contains(searchString)));
            }
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (bool.TryParse(filterStatus, out bool isActive))
                {
                    examsQuery = examsQuery.Where(e => e.IsActive == isActive);
                }
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentStatus"] = filterStatus;
            return View(await examsQuery.OrderByDescending(e => e.Id).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question).ThenInclude(q => q.ReadingPassage)
                .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question).ThenInclude(q => q.ListeningResource)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null) return NotFound();
            return View(exam);
        }

        public IActionResult Create() => View();
        [HttpPost, ValidateAntiForgeryToken]
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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpPost, ValidateAntiForgeryToken]
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

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null) _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        #endregion

        // =========================================================================
        // TÍNH NĂNG MỚI: QUẢN LÝ CÂU HỎI TRONG ĐỀ
        // =========================================================================

        // 1. FORM CẤU HÌNH TẠO ĐỀ TỰ ĐỘNG (GET) - Giữ nguyên
        [HttpGet]
        public async Task<IActionResult> ConfigureAutoGenerate(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            await PrepareQuestionViewBag();
            ViewData["ExamId"] = id;
            ViewData["ExamTitle"] = exam.Title;

            return View();
        }

        // 2. TẠO ĐỀ TỰ ĐỘNG (RANDOM THEO LEVEL) - POST - Giữ nguyên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoGenerate(int id, int targetLevel)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            if (targetLevel < 1 || targetLevel > 5) targetLevel = 3;

            var randomReading = await _context.ReadingPassages
                .Include(r => r.Questions.Where(q => q.Level == targetLevel))
                .Where(r => r.Questions.Any(q => q.Level == targetLevel))
                .OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync();

            var randomListening = await _context.ListeningResources
                .Include(l => l.Questions.Where(q => q.Level == targetLevel))
                .Where(l => l.Questions.Any(q => q.Level == targetLevel))
                .OrderBy(l => Guid.NewGuid()).FirstOrDefaultAsync();

            var randomSpeaking = await _context.Questions
                .Where(q => q.Type == QuestionType.Speaking && q.Level == targetLevel)
                .OrderBy(q => Guid.NewGuid()).FirstOrDefaultAsync();

            var randomWriting = await _context.Questions
                .Where(q => q.Type == QuestionType.Writing && q.Level == targetLevel)
                .OrderBy(q => Guid.NewGuid()).FirstOrDefaultAsync();

            var questionsToAdd = new List<Question>();
            if (randomReading != null) questionsToAdd.AddRange(randomReading.Questions.Where(q => q.Level == targetLevel));
            if (randomListening != null) questionsToAdd.AddRange(randomListening.Questions.Where(q => q.Level == targetLevel));
            if (randomSpeaking != null) questionsToAdd.Add(randomSpeaking);
            if (randomWriting != null) questionsToAdd.Add(randomWriting);

            await AddQuestionsToExam(id, questionsToAdd);
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // 3. CHỌN CÂU HỎI THỦ CÔNG (Theo cấu trúc 4 phần) - GET (ĐÃ FIX LỌC RỖNG)
        [HttpGet]
        public async Task<IActionResult> AddQuestions(int? id, string searchString, int? filterLevel, int? filterTopicId)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            await PrepareQuestionViewBag();
            ViewData["ExamTitle"] = exam.Title;
            ViewData["ExamId"] = id;

            // Lọc Speaking/Writing (Logic giữ nguyên)
            var speakingQuery = _context.Questions
                .Include(q => q.QuestionTopics).ThenInclude(qt => qt.Topic)
                .Where(q => q.Type == QuestionType.Speaking).AsQueryable();

            var writingQuery = _context.Questions
                .Include(q => q.QuestionTopics).ThenInclude(qt => qt.Topic)
                .Where(q => q.Type == QuestionType.Writing).AsQueryable();

            if (filterLevel.HasValue)
            {
                speakingQuery = speakingQuery.Where(q => q.Level == filterLevel);
                writingQuery = writingQuery.Where(q => q.Level == filterLevel);
            }
            if (filterTopicId.HasValue)
            {
                speakingQuery = speakingQuery.Where(q => q.QuestionTopics.Any(qt => qt.TopicId == filterTopicId));
                writingQuery = writingQuery.Where(q => q.QuestionTopics.Any(qt => qt.TopicId == filterTopicId));
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                speakingQuery = speakingQuery.Where(q => q.Content.Contains(searchString));
                writingQuery = writingQuery.Where(q => q.Content.Contains(searchString));
            }

            // --- FIX: Tải Bài đọc/Nghe chỉ có câu hỏi con ---

            // 1. Lấy ID của các ReadingPassage đã được gán
            var passageIdsWithQuestions = await _context.Questions
                .Where(q => q.ReadingPassageId.HasValue)
                .Select(q => q.ReadingPassageId.Value)
                .Distinct()
                .ToListAsync();

            ViewBag.ReadingPassages = await _context.ReadingPassages
                .Include(r => r.Questions)
                .Where(r => passageIdsWithQuestions.Contains(r.Id))
                .ToListAsync();

            // 2. Lấy ID của các ListeningResource đã được gán
            var listeningIdsWithQuestions = await _context.Questions
                .Where(q => q.ListeningResourceId.HasValue)
                .Select(q => q.ListeningResourceId.Value)
                .Distinct()
                .ToListAsync();

            ViewBag.ListeningResources = await _context.ListeningResources
                .Include(l => l.Questions)
                .Where(l => listeningIdsWithQuestions.Contains(l.Id))
                .ToListAsync();

            ViewBag.SpeakingQuestions = await speakingQuery.ToListAsync();
            ViewBag.WritingQuestions = await writingQuery.ToListAsync();

            // Truyền lại giá trị lọc
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentLevel"] = filterLevel;
            ViewData["CurrentTopicId"] = filterTopicId;

            return View();
        }

        // 4. XỬ LÝ LƯU CÂU HỎI THỦ CÔNG - POST (Giữ nguyên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestionsManual(int examId,
            int? selectedReadingId,
            int? selectedListeningId,
            List<int> selectedSpeakingIds,
            List<int> selectedWritingIds)
        {
            var questionsToAdd = new List<Question>();

            if (selectedReadingId.HasValue)
            {
                var qs = await _context.Questions.Where(q => q.ReadingPassageId == selectedReadingId).ToListAsync();
                questionsToAdd.AddRange(qs);
            }
            if (selectedListeningId.HasValue)
            {
                var qs = await _context.Questions.Where(q => q.ListeningResourceId == selectedListeningId).ToListAsync();
                questionsToAdd.AddRange(qs);
            }

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


        // --- Helper functions (Giữ nguyên) ---
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

        private bool ExamExists(int id) => _context.Exams.Any(e => e.Id == id);

        private async Task PrepareQuestionViewBag()
        {
            var levelItems = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "1 - Dễ" },
                    new SelectListItem { Value = "2", Text = "2 - Trung bình" },
                    new SelectListItem { Value = "3", Text = "3 - Khó" },
                    new SelectListItem { Value = "4", Text = "4 - Khó+" },
                    new SelectListItem { Value = "5", Text = "5 - Rất khó" }
                };
            ViewBag.Levels = new SelectList(levelItems, "Value", "Text");
            ViewBag.Topics = new MultiSelectList(await _context.Topics.ToListAsync(), "Id", "Name");
        }
    }
}