using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Web.Models;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được vào
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. Danh sách người dùng
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();

            // Lấy thêm Role cho từng user để hiển thị (Option)
            // Lưu ý: Logic này có thể chậm nếu user quá đông, cần phân trang
            var userViewModels = new List<UserViewModel>(); // Bạn cần tạo class UserViewModel
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    PhoneNumber = user.PhoneNumber,
                    Roles = string.Join(", ", roles),
                    IsLocked = await _userManager.IsLockedOutAsync(user)
                });
            }

            return View(userViewModels);
        }

        // 2. Khóa / Mở khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // Mở khóa
                TempData["SuccessMessage"] = $"Đã mở khóa {user.Email}";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Khóa vĩnh viễn
                TempData["SuccessMessage"] = $"Đã khóa {user.Email}";
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. Xóa tài khoản (Cẩn thận)
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "Đã xóa người dùng.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}