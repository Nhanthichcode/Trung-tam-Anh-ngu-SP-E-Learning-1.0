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

        #region CRUD CƠ BẢN & INDEX
        public async Task<IActionResult> Index(string searchString, string filterStatus)
        {
            var examsQuery = _context.Exams.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
                examsQuery = examsQuery.Where(e => e.Title.Contains(searchString) || (e.Description != null && e.Description.Contains(searchString)));
            if (!string.IsNullOrEmpty(filterStatus) && bool.TryParse(filterStatus, out bool isActive))
                examsQuery = examsQuery.Where(e => e.IsActive == isActive);

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
            if (ModelState.IsValid) { _context.Add(exam); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
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
                try { _context.Update(exam); await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException) { if (!ExamExists(exam.Id)) return NotFound(); else throw; }
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
        // TÍNH NĂNG: QUẢN LÝ CÂU HỎI TRONG ĐỀ
        // =========================================================================

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoGenerate(int id, int targetLevel)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            // ... (Giữ nguyên logic random nếu bạn đã implement) ...
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // 3. CHỌN CÂU HỎI THỦ CÔNG - GET
        // GET: Exams/AddQuestions
        [HttpGet]
        public async Task<IActionResult> AddQuestions(int? id, string searchString, int? filterLevel, int? filterTopicId)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            await PrepareQuestionViewBag();
            ViewData["ExamTitle"] = exam.Title;
            ViewData["ExamId"] = id;

            // Giữ trạng thái bộ lọc
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentLevel"] = filterLevel;
            ViewData["CurrentTopicId"] = filterTopicId;

            // =============================================================
            // 1. XỬ LÝ BÀI ĐỌC (READING) - GHÉP DỮ LIỆU THỦ CÔNG
            // =============================================================

            // B1: Lấy danh sách Bài đọc (theo bộ lọc nếu có)
            var readingQuery = _context.ReadingPassages.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                readingQuery = readingQuery.Where(r => r.Title.Contains(searchString) || r.Content.Contains(searchString));
            }

            var readings = await readingQuery.ToListAsync(); // Tải bài đọc về trước

            // B2: Lấy tất cả câu hỏi con thuộc các bài đọc này
            var readingIds = readings.Select(r => r.Id).ToList();
            var relatedReadingQuestions = await _context.Questions
                                                .Where(q => q.ReadingPassageId.HasValue && readingIds.Contains(q.ReadingPassageId.Value))
                                                .ToListAsync();

            // B3: Ghép câu hỏi vào bài đọc (Loop trong bộ nhớ)
            foreach (var r in readings)
            {
                // Lọc câu hỏi thuộc bài này
                var myQuestions = relatedReadingQuestions.Where(q => q.ReadingPassageId == r.Id).ToList();

                // Áp dụng thêm bộ lọc Level/Topic cho câu hỏi con (nếu cần)
                if (filterLevel.HasValue) myQuestions = myQuestions.Where(q => q.Level == filterLevel).ToList();
                if (filterTopicId.HasValue) myQuestions = myQuestions.Where(q => q.QuestionTopics.Any(qt => qt.TopicId == filterTopicId)).ToList();

                // Gán vào thuộc tính Questions
                r.Questions = myQuestions;
            }

            // B4: Loại bỏ những bài không có câu hỏi nào (nếu muốn) hoặc sau khi lọc bị rỗng
            // readings = readings.Where(r => r.Questions.Any()).ToList(); 

            ViewBag.ReadingPassages = readings;


            // =============================================================
            // 2. XỬ LÝ BÀI NGHE (LISTENING) - GHÉP DỮ LIỆU THỦ CÔNG
            // =============================================================

            var listeningQuery = _context.ListeningResources.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                listeningQuery = listeningQuery.Where(l => l.Title.Contains(searchString));
            }

            var listenings = await listeningQuery.ToListAsync();
            var listeningIds = listenings.Select(l => l.Id).ToList();

            var relatedListeningQuestions = await _context.Questions
                                                  .Where(q => q.ListeningResourceId.HasValue && listeningIds.Contains(q.ListeningResourceId.Value))
                                                  .ToListAsync();

            foreach (var l in listenings)
            {
                var myQuestions = relatedListeningQuestions.Where(q => q.ListeningResourceId == l.Id).ToList();

                if (filterLevel.HasValue) myQuestions = myQuestions.Where(q => q.Level == filterLevel).ToList();
                if (filterTopicId.HasValue) myQuestions = myQuestions.Where(q => q.QuestionTopics.Any(qt => qt.TopicId == filterTopicId)).ToList();

                l.Questions = myQuestions;
            }

            ViewBag.ListeningResources = listenings;


            // =============================================================
            // 3. XỬ LÝ NÓI & VIẾT (Đơn giản - Giữ nguyên)
            // =============================================================
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

            ViewBag.SpeakingQuestions = await speakingQuery.ToListAsync();
            ViewBag.WritingQuestions = await writingQuery.ToListAsync();

            return View();
        }


        // 4. XỬ LÝ LƯU CÂU HỎI THỦ CÔNG - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestionsManual(
            int examId,
            List<int> selectedReadingIds,   // Danh sách ID bài đọc (không dùng trực tiếp để lưu, nhưng có thể dùng để validate)
            List<int> selectedListeningIds, // Danh sách ID bài nghe
            int? selectedSpeakingId,
            int? selectedWritingId,
            List<int> selectedQuestionIds   // QUAN TRỌNG: Danh sách ID câu hỏi con (trắc nghiệm) được tick chọn
        )
        {
            var questionsToAdd = new List<Question>();

            // 1. Xử lý CÂU HỎI CON (Đọc & Nghe)
            if (selectedQuestionIds != null && selectedQuestionIds.Any())
            {
                // Lấy các câu hỏi thực tế từ DB để đảm bảo ID hợp lệ và tránh lỗi
                var qs = await _context.Questions
                    .Where(q => selectedQuestionIds.Contains(q.Id))
                    .ToListAsync();
                questionsToAdd.AddRange(qs);
            }

            // 2. Xử lý SPEAKING (1 bài)
            if (selectedSpeakingId.HasValue)
            {
                var q = await _context.Questions.FindAsync(selectedSpeakingId.Value);
                if (q != null) questionsToAdd.Add(q);
            }

            // 3. Xử lý WRITING (1 bài)
            if (selectedWritingId.HasValue)
            {
                var q = await _context.Questions.FindAsync(selectedWritingId.Value);
                if (q != null) questionsToAdd.Add(q);
            }

            if (questionsToAdd.Any())
            {
                await AddQuestionsToExam(examId, questionsToAdd);
            }

            return RedirectToAction(nameof(Details), new { id = examId });
        }

        // --- Helper functions ---
        private async Task AddQuestionsToExam(int examId, List<Question> questions)
        {
            if (questions == null || !questions.Any()) return;

            // Lấy danh sách ID đã có để tránh trùng lặp
            var existingIds = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .Select(eq => eq.QuestionId)
                .ToListAsync();

            // Tìm max SortOrder hiện tại để xếp câu hỏi mới xuống dưới cùng
            var maxSortOrder = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .MaxAsync(eq => (int?)eq.SortOrder) ?? 0;

            foreach (var q in questions)
            {
                if (!existingIds.Contains(q.Id))
                {
                    maxSortOrder++;
                    _context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = examId,
                        QuestionId = q.Id,
                        Score = 1.0, // Điểm mặc định, có thể sửa sau
                        SortOrder = maxSortOrder
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