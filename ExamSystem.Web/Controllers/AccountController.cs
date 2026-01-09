using ExamSystem.Core.Entities;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment; // Dùng để lấy đường dẫn file

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // --- ĐĂNG KÝ ---
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        if (ModelState.IsValid)
        {
            var user = new AppUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student"); // Mặc định là Student
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }

    // --- ĐĂNG NHẬP ---
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        }
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous] // Quan trọng: Cho phép truy cập ngay cả khi chưa đăng nhập
    public IActionResult Lockout()
    {
        return View();
    }
    // --- GOOGLE LOGIN ---

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi từ dịch vụ ngoài: {remoteError}");
            return View("Login");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ModelState.AddModelError(string.Empty, "Lỗi khi tải thông tin đăng nhập Google.");
            return View("Login");
        }

        var googleAvatarUrl = info.Principal.FindFirstValue("urn:google:picture");

        // Nếu vẫn null, thử tìm fallback (phòng trường hợp Google đổi cấu trúc)
        if (string.IsNullOrEmpty(googleAvatarUrl))
        {
            googleAvatarUrl = info.Principal.FindFirstValue("picture");
        }
        
        // 1. Nếu đã có tài khoản liên kết -> Đăng nhập
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            // (Tùy chọn) Cập nhật lại ảnh Google mới nhất nếu user chưa có ảnh riêng
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            // Điều kiện: User tồn tại + Có ảnh từ Google + (User chưa có ảnh HOẶC đang dùng ảnh Google cũ)
            if (user != null && !string.IsNullOrEmpty(googleAvatarUrl))
            {
                // Chỉ cập nhật nếu user chưa tự upload ảnh riêng (ảnh upload riêng sẽ bắt đầu bằng /uploads/)
                if (string.IsNullOrEmpty(user.AvatarUrl) || !user.AvatarUrl.StartsWith("/uploads/"))
                {
                    // Chỉ update nếu link mới khác link cũ (tránh update thừa)
                    if (user.AvatarUrl != googleAvatarUrl)
                    {
                        user.AvatarUrl = googleAvatarUrl;
                        await _userManager.UpdateAsync(user);
                        // Refresh lại session để header cập nhật ảnh ngay
                        await _signInManager.RefreshSignInAsync(user);
                    }
                }
            }
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut) return RedirectToAction("Lockout");

        // 2. Nếu chưa có tài khoản -> Đăng ký tự động
        else
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // TRƯỜNG HỢP A: Tạo User mới (Lưu luôn AvatarUrl vào đây)
                    user = new AppUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name,
                        AvatarUrl = googleAvatarUrl // [QUAN TRỌNG] Gán ảnh ngay lúc tạo
                    };

                    var resultCreate = await _userManager.CreateAsync(user);

                    if (resultCreate.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                        await _userManager.AddLoginAsync(user, info);
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                else
                {
                    // TRƯỜNG HỢP B: Đã có User cũ -> Liên kết Google
                    // Nếu user cũ chưa có ảnh thì lấy ảnh Google đắp vào
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(googleAvatarUrl))
                    {
                        user.AvatarUrl = googleAvatarUrl;
                        await _userManager.UpdateAsync(user);
                    }

                    var resultAddLogin = await _userManager.AddLoginAsync(user, info);
                    if (resultAddLogin.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }
    }

    // --- ĐĂNG XUẤT ---
    [HttpPost]
    [ValidateAntiForgeryToken] // Bảo mật: Chỉ nhận lệnh từ nút bấm trong trang web
    public async Task<IActionResult> Logout()
    {
        // 1. Xóa Cookie đăng nhập
        await _signInManager.SignOutAsync();

        // 2. (Tùy chọn) Xóa session hoặc các dữ liệu tạm khác nếu có
        // HttpContext.Session.Clear();

        // 3. Chuyển hướng về trang chủ (Home)
        // new { area = "" } để đảm bảo nó về trang chủ gốc, không bị kẹt trong Admin/Student area
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    // --- PROFILE & XÓA ẢNH CŨ ---

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new UserProfileVM
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            AvatarUrl = user.AvatarUrl
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. Xử lý Upload Ảnh Mới
        if (model.AvatarUpload != null)
        {
            // A. [TÍNH NĂNG MỚI] XÓA ẢNH CŨ TRƯỚC KHI LƯU ẢNH MỚI
            // Chỉ xóa nếu ảnh cũ nằm trong thư mục uploads (không xóa ảnh link Google)
            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
            {
                // Chuyển đường dẫn web (/uploads/...) thành đường dẫn ổ cứng (C:\Source\...)
                // TrimStart('/') để bỏ dấu / đầu tiên đi
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath); // Xóa file vật lý
                }
            }

            // B. LƯU ẢNH MỚI
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "user_avatars");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarUpload.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.AvatarUpload.CopyToAsync(fileStream);
            }

            // Cập nhật đường dẫn mới vào User
            user.AvatarUrl = "/uploads/user_avatars/" + uniqueFileName;
        }

        // 2. Cập nhật các thông tin khác
        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.DateOfBirth = model.DateOfBirth;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);

        return View(model);
    }

    // --- XÓA ẢNH ĐẠI DIỆN ---
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. Nếu đang có ảnh
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            // Kiểm tra xem có phải ảnh lưu trên server mình không (bắt đầu bằng /uploads/)
            if (user.AvatarUrl.StartsWith("/uploads/"))
            {
                // Xóa file vật lý để giải phóng bộ nhớ
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // 2. Xóa đường dẫn trong Database (về null)
            user.AvatarUrl = null;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Refresh lại cookie để Header cập nhật ngay lập tức
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đã xóa ảnh đại diện.";
            }
            else
            {
                ModelState.AddModelError("", "Không thể xóa ảnh.");
            }
        }

        return RedirectToAction(nameof(Profile));
    }
}