using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ExamSystem.Web.Controllers
{
    public class ListeningResourcesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ListeningResourcesController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        #region CRUD (Create, Read, Update, Delete)

        // GET: ListeningResources (Đã có lọc)
        public async Task<IActionResult> Index(string searchString)
        {
            var resourcesQuery = _context.ListeningResources.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                resourcesQuery = resourcesQuery.Where(r => r.Title.Contains(searchString) || r.Transcript.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await resourcesQuery.OrderByDescending(r => r.Id).ToListAsync());
        }

        // GET: ListeningResources/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var resource = await _context.ListeningResources.FirstOrDefaultAsync(m => m.Id == id);
            if (resource == null) return NotFound();
            return View(resource);
        }

        // GET: ListeningResources/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ListeningResources/Create (Đã thêm Upload File)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Transcript")] ListeningResource resource, IFormFile audioFile)
        {
            ModelState.Remove("AudioUrl");
            if (ModelState.IsValid)
            {
                if (audioFile != null)
                {
                    if (!IsFileValid(audioFile, out string fileError, allowedExtensions: new[] { ".mp3", ".wav" }))
                    {
                        ModelState.AddModelError("AudioUrl", fileError);
                        return View(resource);
                    }
                    resource.AudioUrl = await UploadFile(audioFile);
                }

                _context.Add(resource);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(resource);
        }

        // GET: ListeningResources/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var resource = await _context.ListeningResources.FindAsync(id);
            if (resource == null) return NotFound();
            return View(resource);
        }

        // POST: ListeningResources/Edit/5 (Đã thêm Upload File)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Transcript,AudioUrl")] ListeningResource resource, IFormFile? audioFile)
        {
            if (id != resource.Id) return NotFound();

            // Loại bỏ Validation cho AudioUrl nếu không upload file mới
            if (audioFile == null)
            {
                ModelState.Remove("AudioUrl");
                var oldResource = await _context.ListeningResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                if (oldResource != null)
                {
                    resource.AudioUrl = oldResource.AudioUrl;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (audioFile != null)
                    {
                        if (!IsFileValid(audioFile, out string fileError, allowedExtensions: new[] { ".mp3", ".wav" }))
                        {
                            ModelState.AddModelError("AudioUrl", fileError);
                            return View(resource);
                        }
                        // Xóa file cũ nếu tồn tại
                        DeleteFile(resource.AudioUrl);
                        resource.AudioUrl = await UploadFile(audioFile);
                    }

                    _context.Update(resource);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ListeningResources.Any(e => e.Id == resource.Id))
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
            return View(resource);
        }

        // GET: ListeningResources/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var resource = await _context.ListeningResources.FirstOrDefaultAsync(m => m.Id == id);
            if (resource == null) return NotFound();
            return View(resource);
        }

        // POST: ListeningResources/Delete/5 (Đã thêm xóa file vật lý và xóa chuỗi)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resource = await _context.ListeningResources.FindAsync(id);
            if (resource == null) return RedirectToAction(nameof(Index));

            // 1. TÌM VÀ XÓA CÂU HỎI CON (EF Core sẽ tự xóa Answers và QuestionTopics)
            var relatedQuestions = await _context.Questions
                .Where(q => q.ListeningResourceId == id)
                .ToListAsync();

            if (relatedQuestions.Any())
            {
                _context.Questions.RemoveRange(relatedQuestions);
            }

            // 2. XÓA FILE VẬT LÝ
            DeleteFile(resource.AudioUrl);

            // 3. XÓA TÀI NGUYÊN GỐC
            _context.ListeningResources.Remove(resource);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helper Methods (Tái sử dụng từ QuestionsController)

        private async Task<string> UploadFile(IFormFile file)
        {
            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "audio");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return "/uploads/audio/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL LOG: Lỗi ghi file: {ex.Message}");
                throw new InvalidOperationException($"Không thể lưu tệp tin. Vui lòng kiểm tra quyền truy cập.", ex);
            }
        }

        private void DeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, path.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi xóa file: {ex.Message}");
                    // Có thể thêm logic ghi log hoặc thông báo lỗi
                }
            }
        }

        private bool IsFileValid(IFormFile file, out string errorMessage, long maxFileSize = 5242880, string[]? allowedExtensions = null)
        {
            if (allowedExtensions == null) allowedExtensions = new[] { ".mp3", ".wav", ".jpg", ".png" };

            if (file.Length > maxFileSize)
            {
                errorMessage = "Tệp quá lớn. Vui lòng chọn tệp dưới 5 MB.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                errorMessage = $"Định dạng tệp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}