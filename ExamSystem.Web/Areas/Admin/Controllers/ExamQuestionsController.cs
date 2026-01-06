using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/exam-questions")] // <--- THÊM DÒNG NÀY: Để tránh lỗi trùng đường dẫn
    // [Authorize(Roles = "Admin" || "Teacher")]
    public class ExamQuestionsController : Controller // <--- SỬA: Đổi ControllerBase thành Controller
    {
        private readonly AppDbContext _context;
        public ExamQuestionsController(AppDbContext context) => _context = context;

        // Thêm câu hỏi vào đề thi (quan trọng)
        // URL: /admin/exam-questions/add
        [HttpPost("add")] // <--- SỬA: Thêm đường dẫn con "add"
        [ValidateAntiForgeryToken] // <--- THÊM: Bảo mật cho form
        public async Task<IActionResult> AddQuestionToExam(ExamQuestion examQuestion) // <--- SỬA: Trả về IActionResult
        {
            if (ModelState.IsValid)
            {
                _context.ExamQuestions.Add(examQuestion);
                await _context.SaveChangesAsync();

                // Sửa: Quay lại trang danh sách hoặc trang chi tiết đề thi thay vì trả về JSON
                // Giả sử bạn muốn quay lại trang chi tiết đề thi
                // return RedirectToAction("Details", "Exams", new { id = examQuestion.ExamId });
                return RedirectToAction("Index", "Home"); // Tạm thời quay về Home nếu chưa có trang khác
            }
            return View(examQuestion); // Nếu lỗi thì hiện lại View
        }

        // Lấy chi tiết 1 dòng gán (thường để check)
        // URL: /admin/exam-questions/detail/{id}
        [HttpGet("detail/{id}")] // <--- SỬA: Đặt tên route rõ ràng "detail"
        public async Task<IActionResult> GetExamQuestion(int id) // <--- SỬA: Trả về IActionResult
        {
            var examQuestion = await _context.ExamQuestions
                .Include(eq => eq.Question) // Load nội dung câu hỏi
                .FirstOrDefaultAsync(eq => eq.Id == id);

            if (examQuestion == null) return NotFound();

            return View(examQuestion); // <--- SỬA: Trả về View để hiển thị giao diện
        }

        // Xóa câu hỏi khỏi đề thi
        // URL: /admin/exam-questions/delete/{id}
        // Lưu ý: Web dùng Post để xóa (thông qua Form), API mới dùng Delete
        [HttpPost("delete/{id}")] // <--- SỬA: Đổi HttpDelete thành HttpPost
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveQuestionFromExam(int id)
        {
            var examQuestion = await _context.ExamQuestions.FindAsync(id);
            if (examQuestion == null) return NotFound();

            _context.ExamQuestions.Remove(examQuestion);
            await _context.SaveChangesAsync();

            // Sửa: Xóa xong thì load lại trang (thường là trang chi tiết đề thi)
            // return RedirectToAction("Details", "Exams", new { id = examQuestion.ExamId });
            return RedirectToAction("Index", "Home");
        }
    }
}