using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
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
        public IActionResult Create()
        {
            ViewData["ExamId"] = new SelectList(_context.Exams, "Id", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamPart examPart)
        {
            if (ModelState.IsValid)
            {
                _context.Add(examPart);
                await _context.SaveChangesAsync();
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
    }
}