using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc; // Dùng cho MVC
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;

namespace ExamSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Route("student/test-results")] // Đặt đường dẫn cứng để tránh xung đột với Admin
    // [Authorize(Roles = "Student")] // Bỏ comment khi bạn muốn bật chức năng đăng nhập
    public class TestResultsController : Controller
    {
        private readonly AppDbContext _context;

        public TestResultsController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. Xem danh sách kết quả ---
        // URL: /student/test-results
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách kết quả (nên lọc theo User đang đăng nhập nếu cần)
            var results = await _context.TestResults.ToListAsync();
            return View(results); // Trả về View: Areas/Student/Views/TestResults/Index.cshtml
        }

        // --- 2. Xem chi tiết một kết quả ---
        // URL: /student/test-results/detail/5
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0) return NotFound();

            var testResult = await _context.TestResults
                .FirstOrDefaultAsync(m => m.Id == id);

            if (testResult == null) return NotFound();

            return View(testResult); // Trả về View: Areas/Student/Views/TestResults/Details.cshtml
        }

        // --- 3. Sửa kết quả (Lưu ý: Thường sinh viên không được sửa kết quả thi, nhưng mình vẫn convert theo code cũ của bạn) ---

        // GET: Hiển thị form sửa
        // URL: /student/test-results/edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);
            if (testResult == null) return NotFound();
            return View(testResult);
        }

        // POST: Lưu dữ liệu sửa
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken] // Bảo mật cho Web
        public async Task<IActionResult> Edit(int id, TestResult testResult)
        {
            if (id != testResult.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(testResult);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestResultExists(testResult.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index)); // Sửa xong quay về danh sách
            }
            return View(testResult); // Nếu lỗi thì hiện lại form cũ
        }

        // --- 4. Tạo mới (Create) ---

        // GET: Hiển thị form tạo mới
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Lưu dữ liệu mới
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestResult testResult)
        {
            if (ModelState.IsValid)
            {
                _context.Add(testResult);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(testResult);
        }

        // --- 5. Xóa (Delete) ---

        // GET: Xác nhận xóa
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var testResult = await _context.TestResults.FirstOrDefaultAsync(m => m.Id == id);
            if (testResult == null) return NotFound();

            return View(testResult); // Trả về View xác nhận xóa
        }

        // POST: Thực hiện xóa
        [HttpPost("delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);
            if (testResult != null)
            {
                _context.TestResults.Remove(testResult);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TestResultExists(int id)
        {
            return _context.TestResults.Any(e => e.Id == id);
        }
    }
}