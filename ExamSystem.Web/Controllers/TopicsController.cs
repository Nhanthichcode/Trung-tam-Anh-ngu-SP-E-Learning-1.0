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
    public class TopicsController : Controller
    {
        private readonly AppDbContext _context;

        public TopicsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Topics
        public async Task<IActionResult> Index()
        {
            return View(await _context.Topics.ToListAsync());
        }

        // GET: Topics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // GET: Topics/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Topic topic)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(topic);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, id = topic.Id });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi lưu dữ liệu vào cơ sở dữ liệu." });
                }
            }
            // Nếu ModelState không hợp lệ
            return Json(new { success = false, message = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại Tên chủ đề." });
        }
        // GET: Topics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }
            return View(topic);
        }

        // POST: Topics/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Topic topic)
        {
            if (id != topic.Id)
            {
                return Json(new { success = false, message = "ID không khớp." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(topic);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Topics.Any(e => e.Id == topic.Id))
                    {
                        return Json(new { success = false, message = "Không tìm thấy Chủ đề." });
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật dữ liệu." });
                }
            }
            return Json(new { success = false, message = "Dữ liệu nhập không hợp lệ." });
        }
        // GET: Topics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // 3. Sửa hàm DELETECONFIRMED (POST)
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic != null)
            {
                try
                {
                    _context.Topics.Remove(topic);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi xóa: Chủ đề này có thể đang được sử dụng." });
                }
            }
            return Json(new { success = false, message = "Không tìm thấy Chủ đề để xóa." });
        }
        private bool TopicExists(int id)
        {
            return _context.Topics.Any(e => e.Id == id);
        }
    }
}
