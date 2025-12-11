using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
    public class ExamsController : Controller
    {
        private readonly AppDbContext _context;

        public ExamsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH (INDEX)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams.OrderByDescending(e => e.StartDate).ToListAsync();
            return View(exams);
        }

        // GET: Exams/Manage/5 (Giao diện soạn đề)
        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.ExamParts)
                .ThenInclude(ep => ep.ExamQuestions)
                .ThenInclude(eq => eq.Question) // Load câu hỏi đã có trong đề
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exam == null) return NotFound();

            // Load danh sách câu hỏi từ ngân hàng để chọn thêm vào
            ViewBag.AllQuestions = await _context.Questions.Take(50).ToListAsync(); // Load tạm 50 câu

            return View(exam);
        }

        // POST: Thêm câu hỏi vào phần thi
        [HttpPost]
        public async Task<IActionResult> AddQuestionToPart(int examId, int partId, int questionId, float score)
        {
            var exists = await _context.ExamQuestions
                .AnyAsync(eq => eq.ExamPartId == partId && eq.QuestionId == questionId);

            if (!exists)
            {
                var link = new ExamQuestion
                {
                    ExamPartId = partId,
                    QuestionId = questionId,
                    Score = score,
                    SortOrder = 0 // Mặc định
                };
                _context.ExamQuestions.Add(link);
                await _context.SaveChangesAsync();
            }

            // Quay lại trang Manage
            return RedirectToAction("Manage", new { id = examId });
        }

        // POST: Xóa câu hỏi khỏi phần thi
        [HttpPost]
        public async Task<IActionResult> RemoveQuestionFromPart(int examId, int examQuestionId)
        {
            var link = await _context.ExamQuestions.FindAsync(examQuestionId);
            if (link != null)
            {
                _context.ExamQuestions.Remove(link);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Manage", new { id = examId });
        }

        // ==========================================
        // 2. TẠO MỚI (CREATE)
        // ==========================================

        // GET: Hiển thị form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Xử lý lưu dữ liệu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam exam)
        {
            if (ModelState.IsValid)
            {
                // Xử lý logic mặc định (nếu cần)
                if (exam.StartDate == default) exam.StartDate = DateTime.Now;

                _context.Add(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // ==========================================
        // 3. CHỈNH SỬA (EDIT)
        // ==========================================

        // GET: Hiển thị form sửa kèm dữ liệu cũ
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            return View(exam);
        }

        // POST: Lưu thay đổi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam exam)
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

        // ==========================================
        // 4. XÓA (DELETE)
        // ==========================================

        // GET: Hiển thị trang xác nhận xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null) return NotFound();

            return View(exam);
        }

        // POST: Xóa thật sự
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra tồn tại
        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
    }
}