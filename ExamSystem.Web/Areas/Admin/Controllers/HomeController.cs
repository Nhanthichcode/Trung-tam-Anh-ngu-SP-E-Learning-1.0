using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    // [Authorize(Roles = "Admin" || "Teacher")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var studentRoleId = await _context.Roles
        .Where(r => r.Name == "Student")
        .Select(r => r.Id)
        .FirstOrDefaultAsync();

            // 2. Đếm số user có RoleId đó trong bảng trung gian
            int studentCount = 0;
            if (studentRoleId != null)
            {
                studentCount = await _context.UserRoles
                    .CountAsync(ur => ur.RoleId == studentRoleId);
            }
            var model = new DashboardViewModel
            {
                // Đếm số liệu (Demo logic - bạn thay bằng query DB thực tế)               
                TotalQuestions = await _context.Questions.CountAsync(),
                TotalExams = await _context.Exams.CountAsync(),


                // Đếm số bài thi có câu tự luận chưa chấm
                PendingGrades = await _context.TestAttempts
                    .CountAsync(ta => ta.Status == (int)TestStatus.Submitted && !ta.isGraded),

                // Lấy 5 bài thi gần nhất
                RecentAttempts = await _context.TestAttempts
                    .Include(ta => ta.User)
                    .Include(ta => ta.Exam)
                    .OrderByDescending(ta => ta.SubmitTime)
                    .Take(5)
                    .Select(ta => new RecentAttempt
                    {
                        Id = ta.Id,
                        StudentName = ta.User.FullName,
                        ExamTitle = ta.Exam.Title,
                        SubmitTime = ta.SubmitTime ?? DateTime.Now,
                        Score = ta.Score,
                        IsGraded = ta.Status == (int)TestStatus.Graded
                    }).ToListAsync()
            };

            return View(model);
        }
    }
}