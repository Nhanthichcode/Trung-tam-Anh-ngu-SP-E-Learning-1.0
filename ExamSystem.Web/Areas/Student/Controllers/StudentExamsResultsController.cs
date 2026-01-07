using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc; // Dùng cho MVC
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    // [Authorize(Roles = "Student")] // Bỏ comment khi bạn muốn bật chức năng đăng nhập
    public class StudentExamsResultsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public StudentExamsResultsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. Xem danh sách kết quả ---
        // URL: /student/test-results
        public async Task<IActionResult> Index()
        {
            // 1. Lấy ID người dùng hiện tại
            var userId = _userManager.GetUserId(User);

            // 2. Lấy danh sách LƯỢT THI (TestAttempt) thay vì TestResult
            var attempts = await _context.TestAttempts
                .Include(ta => ta.Exam) // Để lấy tên đề thi
                .Where(ta => ta.UserId == userId)
                .OrderByDescending(ta => ta.SubmitTime)
                .ToListAsync();

            // 3. Gửi 'attempts' (kiểu List<TestAttempt>) sang View
            return View(attempts);
        }

        // --- 2. Xem chi tiết một kết quả ---
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0) return NotFound();

            var testResult = await _context.TestAttempts
                .FirstOrDefaultAsync(m => m.Id == id);

            if (testResult == null) return NotFound();

            return View(testResult); // Trả về View: Areas/Student/Views/TestResults/Details.cshtml
        }

        // --- 3. Sửa kết quả (Lưu ý: Thường sinh viên không được sửa kết quả thi, nhưng mình vẫn convert theo code cũ của bạn) ---

        // GET: Hiển thị form sửa
        public async Task<IActionResult> Edit(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);
            if (testResult == null) return NotFound();
            return View(testResult);
        }

        // POST: Lưu dữ liệu sửa
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