using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;

//Environment.SetEnvironmentVariable("EPPlusLicenseContext", "NonCommercial"); 
OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 2. Cấu hình Identity (Đăng nhập/Đăng ký)
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Cấu hình password đơn giản cho dễ test (Tùy chọn)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, ExamSystem.Web.Services.EmailSender>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Để load được CSS, JS, hình ảnh trong wwwroot

app.UseRouting();

app.UseAuthentication(); // Bắt buộc có dòng này để đăng nhập
app.UseAuthorization();  // Bắt buộc có dòng này để phân quyền

// --- ĐÂY LÀ PHẦN BẠN ĐANG THIẾU ---
// Dòng này chỉ định: Nếu không gõ gì cả, hãy vào Home Controller -> trang Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Gọi hàm Seed data
        await ExamSystem.Infrastructure.Seeding.DbSeeder.SeedAllAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi nạp dữ liệu mẫu (Seeding).");
    }
}
//

app.Run();