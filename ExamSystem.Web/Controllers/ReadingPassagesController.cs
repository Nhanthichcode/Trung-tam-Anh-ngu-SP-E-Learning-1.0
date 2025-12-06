using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;

namespace ExamSystem.Web.Controllers
{
    public class ReadingPassagesController : Controller
    {
        private readonly AppDbContext _context;

        public ReadingPassagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ReadingPassages
        public async Task<IActionResult> Index()
        {
            return View(await _context.ReadingPassages.ToListAsync());
        }

        // GET: ReadingPassages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var readingPassage = await _context.ReadingPassages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (readingPassage == null)
            {
                return NotFound();
            }

            return View(readingPassage);
        }

        // GET: ReadingPassages/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ReadingPassages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Content")] ReadingPassage readingPassage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(readingPassage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(readingPassage);
        }

        // GET: ReadingPassages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var readingPassage = await _context.ReadingPassages.FindAsync(id);
            if (readingPassage == null)
            {
                return NotFound();
            }
            return View(readingPassage);
        }

        // POST: ReadingPassages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content")] ReadingPassage readingPassage)
        {
            if (id != readingPassage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(readingPassage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReadingPassageExists(readingPassage.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(readingPassage);
        }

        // GET: ReadingPassages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var readingPassage = await _context.ReadingPassages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (readingPassage == null)
            {
                return NotFound();
            }

            return View(readingPassage);
        }

        // POST: ReadingPassages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var readingPassage = await _context.ReadingPassages.FindAsync(id);
            if (readingPassage != null)
            {
                _context.ReadingPassages.Remove(readingPassage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReadingPassageExists(int id)
        {
            return _context.ReadingPassages.Any(e => e.Id == id);
        }
    }
}
