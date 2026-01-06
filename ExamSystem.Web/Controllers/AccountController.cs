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

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

    // 1. Gửi yêu cầu sang Google
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

        // 1. Thử đăng nhập nếu tài khoản Google này ĐÃ ĐƯỢC LIÊN KẾT trước đó
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToAction("Lockout");
        }

        // 2. Nếu chưa liên kết -> Xử lý logic Đăng ký hoặc Liên kết
        else
        {
            // Lấy email từ thông tin Google trả về
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (email != null)
            {
                // BƯỚC QUAN TRỌNG: Kiểm tra xem email này đã có trong Database chưa?
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // TRƯỜNG HỢP A: Chưa có tài khoản nào -> TẠO MỚI
                    user = new AppUser { UserName = email, Email = email, FullName = name };
                    var resultCreate = await _userManager.CreateAsync(user);

                    if (resultCreate.Succeeded)
                    {
                        // Gán Role (Cần đảm bảo Role "Student" đã có trong bảng Roles)
                        await _userManager.AddToRoleAsync(user, "Student");

                        // Liên kết user mới với Google
                        await _userManager.AddLoginAsync(user, info);

                        // Đăng nhập luôn
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        // Nếu tạo lỗi (ví dụ mật khẩu không đủ mạnh - dù ở đây ko set pass)
                        foreach (var error in resultCreate.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    // TRƯỜNG HỢP B: Đã có tài khoản (đăng ký bằng pass thường) -> LIÊN KẾT VÀO
                    var resultAddLogin = await _userManager.AddLoginAsync(user, info);

                    if (resultAddLogin.Succeeded)
                    {
                        // Liên kết thành công -> Đăng nhập
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không thể liên kết tài khoản Google.");
                    }
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }
    }

    // --- ĐĂNG XUẤT ---
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}