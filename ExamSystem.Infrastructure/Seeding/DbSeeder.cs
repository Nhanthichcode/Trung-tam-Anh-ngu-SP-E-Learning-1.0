using ExamSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExamSystem.Infrastructure.Seeding
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Lấy dịch vụ quản lý Role và User từ hệ thống
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // 1. DANH SÁCH CÁC QUYỀN CẦN TẠO
            string[] roleNames = { "Admin", "Teacher", "Student" };

            foreach (var roleName in roleNames)
            {
                // Nếu quyền chưa tồn tại thì tạo mới
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. TẠO TÀI KHOẢN ADMIN MẶC ĐỊNH (Nếu chưa có)
            var adminEmail = "admin@exam.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên Hệ thống",
                    EmailConfirmed = true // Xác nhận email luôn để đăng nhập được ngay
                };

                // Tạo user với mật khẩu mặc định (Lưu ý: Phải có chữ hoa, thường, số, ký tự đặc biệt)
                var createPowerUser = await userManager.CreateAsync(newAdmin, "Admin@123");

                if (createPowerUser.Succeeded)
                {
                    // Gán quyền Admin cho user này
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}