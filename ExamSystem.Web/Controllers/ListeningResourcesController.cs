using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
    public class ListeningResourcesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ListeningResourcesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // INDEX
        public async Task<IActionResult> Index()
        {
            return View(await _context.ListeningResources.ToListAsync());
        }

        // CREATE
        public IActionResult Create() => View();

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListeningResource listeningResource, IFormFile? audioFile)
        {
            // 1. Xử lý Upload file
            if (audioFile != null && audioFile.Length > 0)
            {
                // Tạo tên file độc nhất để tránh trùng
                var fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(audioFile.FileName);

                // Đường dẫn lưu file: wwwroot/uploads/audio
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "audio");

                // Tạo thư mục nếu chưa có
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                // Copy file vào server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                // Lưu đường dẫn tương đối vào Database
                listeningResource.AudioUrl = "/uploads/audio/" + fileName;
            }
            else if (string.IsNullOrEmpty(listeningResource.AudioUrl))
            {
                ModelState.AddModelError("AudioUrl", "Vui lòng upload file hoặc nhập link.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(listeningResource);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(listeningResource);
        }

        // EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ListeningResources.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ListeningResource listeningResource)
        {
            if (id != listeningResource.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(listeningResource);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ListeningResources.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(listeningResource);
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ListeningResources.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.ListeningResources.FindAsync(id);
            if (item != null) _context.ListeningResources.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}