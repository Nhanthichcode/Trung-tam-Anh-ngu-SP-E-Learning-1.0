using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    // [Authorize(Roles = "Admin" || "Teacher")]
    public class ExamPartsController : Controller
    {
        private readonly AppDbContext _context;
        public ExamPartsController(AppDbContext context) => _context = context;

        // INDEX
        public async Task<IActionResult> Index()
        {
            var parts = await _context.ExamParts.Include(e => e.Exam).ToListAsync();
            return View(parts);
        }

        // CREATE
        // 1. Create GET: Nhận examId từ URL (nếu có)
        public IActionResult Create(int? examId)
        {
            // Nếu có examId truyền vào, ta chọn sẵn trong Dropdown
            ViewData["ExamId"] = new SelectList(_context.Exams, "Id", "Title", examId);

            // Gửi examId sang View để lát nữa biết đường quay lại
            ViewBag.ReturnExamId = examId;

            return View();
        }

        // 2. Create POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamPart examPart)
        {
            if (ModelState.IsValid)
            {
                _context.Add(examPart);
                await _context.SaveChangesAsync();

                // TỐI ƯU: Nếu tạo từ trang Soạn đề, hãy quay lại trang Soạn đề
                if (examPart.ExamId > 0)
                {
                    return RedirectToAction("Manage", "Exams", new { id = examPart.ExamId });
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["ExamId"] = new SelectList(_context.Exams, "Id", "Title", examPart.ExamId);
            return View(examPart);
        }


        // EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var examPart = await _context.ExamParts.FindAsync(id);
            if (examPart == null) return NotFound();

            ViewData["ExamId"] = new SelectList(_context.Exams, "Id", "Title", examPart.ExamId);
            return View(examPart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamPart examPart)
        {
            if (id != examPart.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(examPart);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ExamId"] = new SelectList(_context.Exams, "Id", "Title", examPart.ExamId);
            return View(examPart);
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var examPart = await _context.ExamParts.Include(e => e.Exam).FirstOrDefaultAsync(m => m.Id == id);
            return examPart == null ? NotFound() : View(examPart);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var examPart = await _context.ExamParts.FindAsync(id);
            if (examPart != null) _context.ExamParts.Remove(examPart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePartAjax(int id)
        {
            var part = await _context.ExamParts.FindAsync(id);
            if (part == null) return Json(new { success = false, message = "Không tìm thấy phần thi!" });

            _context.ExamParts.Remove(part);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ClearPartQuestionsAjax(int id)
        {
            var part = await _context.ExamParts.Include(p => p.ExamQuestions).FirstOrDefaultAsync(x => x.Id == id);
            if (part == null) return Json(new { success = false, message = "Không tìm thấy phần thi!" });

            _context.ExamQuestions.RemoveRange(part.ExamQuestions);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}