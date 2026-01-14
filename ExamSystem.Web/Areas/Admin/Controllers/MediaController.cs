using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MediaController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public MediaController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // 1. Duyệt file
        // path: Đường dẫn tương đối (ví dụ: "user_avatars" hoặc "test/770d...")
        public IActionResult Index(string path = "")
        {
            // Root thật: D:\ExamSystem\ExamSystem.Web\wwwroot\uploads
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");

            // Đường dẫn hiện tại đang xem
            var currentPath = Path.Combine(uploadsRoot, path);

            // Bảo mật: Chống hack đường dẫn (Directory Traversal)
            if (!currentPath.StartsWith(uploadsRoot))
            {
                return BadRequest("Truy cập trái phép!");
            }

            if (!Directory.Exists(currentPath))
            {
                return NotFound($"Thư mục không tồn tại: {path}");
            }

            // Lấy danh sách thư mục con
            var directories = Directory.GetDirectories(currentPath)
                .Select(d => new FileItemViewModel
                {
                    Name = Path.GetFileName(d),
                    Path = Path.GetRelativePath(uploadsRoot, d).Replace("\\", "/"), // Chuyển \ thành / cho web
                    IsDirectory = true
                });

            // Lấy danh sách file
            var files = Directory.GetFiles(currentPath)
                .Select(f => new FileItemViewModel
                {
                    Name = Path.GetFileName(f),
                    Path = Path.GetRelativePath(uploadsRoot, f).Replace("\\", "/"),
                    IsDirectory = false,
                    Size = new FileInfo(f).Length,
                    Extension = Path.GetExtension(f).ToLower()
                });

            var model = new MediaManagerViewModel
            {
                CurrentPath = path,
                Items = directories.Concat(files).ToList()
            };

            return View(model);
        }

        // 2. Xóa file hoặc thư mục
        [HttpPost]
        public IActionResult Delete(string path)
        {
            if (string.IsNullOrEmpty(path)) return BadRequest();

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
            var fullPath = Path.Combine(uploadsRoot, path);

            // Bảo mật
            if (!fullPath.StartsWith(uploadsRoot)) return BadRequest();

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                TempData["SuccessMessage"] = "Đã xóa file.";
            }
            else if (Directory.Exists(fullPath))
            {
                try
                {
                    Directory.Delete(fullPath, true); // true = xóa đệ quy cả file bên trong
                    TempData["SuccessMessage"] = "Đã xóa thư mục.";
                }
                catch
                {
                    TempData["ErrorMessage"] = "Không thể xóa thư mục (có thể đang được sử dụng).";
                }
            }

            // Quay lại thư mục cha
            var parentDir = Path.GetDirectoryName(path);
            return RedirectToAction(nameof(Index), new { path = parentDir });
        }
    }

    // ViewModel hỗ trợ hiển thị
    public class MediaManagerViewModel
    {
        public string CurrentPath { get; set; }
        public List<FileItemViewModel> Items { get; set; }
    }

    public class FileItemViewModel
    {
        public string Name { get; set; }
        public string Path { get; set; } // Đường dẫn tương đối để dùng trong URL
        public bool IsDirectory { get; set; }
        public long Size { get; set; } // Byte
        public string Extension { get; set; }

        // Helper để hiển thị icon
        public string Icon => IsDirectory ? "bi-folder-fill text-warning" :
                              Extension == ".jpg" || Extension == ".png" ? "bi-file-image text-success" :
                              Extension == ".mp3" ? "bi-file-music text-info" : "bi-file-earmark";

        // Helper tính size MB/KB
        public string SizeDisplay => IsDirectory ? "-" :
                                     Size > 1024 * 1024 ? $"{(Size / 1024f / 1024f):F2} MB" :
                                     $"{(Size / 1024f):F2} KB";
    }
}