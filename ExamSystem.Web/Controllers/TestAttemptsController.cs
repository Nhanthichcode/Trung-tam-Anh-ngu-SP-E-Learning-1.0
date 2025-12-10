using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
    public class TestAttemptsController : Controller
    {
        private readonly AppDbContext _context;
        public TestAttemptsController(AppDbContext context) => _context = context;

        // INDEX: Xem danh sách người thi
        public async Task<IActionResult> Index()
        {
            var attempts = await _context.TestAttempts
                .Include(t => t.Exam)
                //.Include(t => t.User) // Mở dòng này nếu bạn có bảng User và đã setup quan hệ
                .OrderByDescending(t => t.StartTime)
                .ToListAsync();
            return View(attempts);
        }

        // DETAILS: Xem chi tiết bài làm
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var attempt = await _context.TestAttempts
                .Include(t => t.Exam)
                .Include(t => t.TestResults) // Load kết quả từng câu
                .ThenInclude(tr => tr.Question) // Load nội dung câu hỏi để hiển thị
                .FirstOrDefaultAsync(m => m.Id == id);

            if (attempt == null) return NotFound();
            return View(attempt);
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var attempt = await _context.TestAttempts.Include(t => t.Exam).FirstOrDefaultAsync(m => m.Id == id);
            return attempt == null ? NotFound() : View(attempt);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attempt = await _context.TestAttempts.FindAsync(id);
            if (attempt != null) _context.TestAttempts.Remove(attempt);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}