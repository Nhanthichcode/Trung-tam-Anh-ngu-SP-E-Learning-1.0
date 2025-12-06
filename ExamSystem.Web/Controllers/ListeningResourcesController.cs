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
    public class ListeningResourcesController : Controller
    {
        private readonly AppDbContext _context;

        public ListeningResourcesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ListeningResources
        public async Task<IActionResult> Index()
        {
            return View(await _context.ListeningResources.ToListAsync());
        }

        // GET: ListeningResources/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listeningResource = await _context.ListeningResources
                .FirstOrDefaultAsync(m => m.Id == id);
            if (listeningResource == null)
            {
                return NotFound();
            }

            return View(listeningResource);
        }

        // GET: ListeningResources/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ListeningResources/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,AudioUrl,Transcript")] ListeningResource listeningResource)
        {
            if (ModelState.IsValid)
            {
                _context.Add(listeningResource);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(listeningResource);
        }

        // GET: ListeningResources/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listeningResource = await _context.ListeningResources.FindAsync(id);
            if (listeningResource == null)
            {
                return NotFound();
            }
            return View(listeningResource);
        }

        // POST: ListeningResources/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AudioUrl,Transcript")] ListeningResource listeningResource)
        {
            if (id != listeningResource.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(listeningResource);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ListeningResourceExists(listeningResource.Id))
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
            return View(listeningResource);
        }

        // GET: ListeningResources/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listeningResource = await _context.ListeningResources
                .FirstOrDefaultAsync(m => m.Id == id);
            if (listeningResource == null)
            {
                return NotFound();
            }

            return View(listeningResource);
        }

        // POST: ListeningResources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var listeningResource = await _context.ListeningResources.FindAsync(id);
            if (listeningResource != null)
            {
                _context.ListeningResources.Remove(listeningResource);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ListeningResourceExists(int id)
        {
            return _context.ListeningResources.Any(e => e.Id == id);
        }
    }
}
