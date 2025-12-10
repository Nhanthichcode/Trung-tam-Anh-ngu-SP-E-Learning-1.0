using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần cái này cho SelectList
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly AppDbContext _context;

        public QuestionsController(AppDbContext context) => _context = context;

        // INDEX
        public async Task<IActionResult> Index()
        {
            var questions = await _context.Questions
                .Include(q => q.ReadingPassage)   // Load tên bài đọc
                .Include(q => q.ListeningResource)// Load tên bài nghe
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();
            return View(questions);
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            // Nạp danh sách Bài đọc & Bài nghe vào ViewBag để hiển thị Dropdown chọn
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title");
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title");
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Question question)
        {
            // Logic tự động xác định loại câu hỏi (nếu muốn)
            if (question.ReadingPassageId != null)
            {
                question.SkillType = Core.Enums.ExamSkill.Reading; // Reading
            }
            else if (question.ListeningResourceId != null)
            {
                question.SkillType = Core.Enums.ExamSkill.Listening; // Listening
            }

            if (ModelState.IsValid)
            {
                question.CreatedDate = DateTime.Now;
                _context.Add(question);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, nạp lại Dropdown
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", question.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", question.ListeningResourceId);
            return View(question);
        }

        // EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", question.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", question.ListeningResourceId);
            return View(question);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question)
        {
            if (id != question.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", question.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", question.ListeningResourceId);
            return View(question);
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions
                .Include(q => q.ReadingPassage)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null) return NotFound();
            return View(question);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id) => _context.Questions.Any(e => e.Id == id);
    }
}