using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần cái này cho SelectList
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ExamSystem.Web.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public QuestionsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // INDEX
        public async Task<IActionResult> Index()
        {
            var questions = await _context.Questions
                .Include(q => q.ReadingPassage)   // Load tên bài đọc
                .Include(q => q.ListeningResource)// Load tên bài nghe
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();
            return View(questions);
        }

        // GET: Hiển thị trang upload
        public IActionResult Import()
        {
            return View();
        }

        // POST: Xử lý file Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn file Excel");
                return View();
            }
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    // Lấy Sheet đầu tiên
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    // Duyệt từ dòng 2 (bỏ dòng tiêu đề)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var content = worksheet.Cells[row, 1].Value?.ToString().Trim();
                        if (string.IsNullOrEmpty(content)) continue;

                        // Tạo câu hỏi
                        var question = new Question
                        {
                            Content = content,
                            SkillType = (Core.Enums.ExamSkill)int.Parse(worksheet.Cells[row, 2].Value?.ToString() ?? "1"), // Cột 2: Kỹ năng
                            Level = int.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? "1"),     // Cột 3: Độ khó
                            CreatedDate = DateTime.Now,
                            Answers = new List<Answer>()
                        };

                        // Đọc 4 đáp án (Cột 4, 5, 6, 7)
                        // Cột 8: Số thứ tự đáp án đúng (1, 2, 3 hoặc 4)
                        int correctIndex = int.Parse(worksheet.Cells[row, 8].Value?.ToString() ?? "1");

                        for (int i = 1; i <= 4; i++)
                        {
                            question.Answers.Add(new Answer
                            {
                                Content = worksheet.Cells[row, 3 + i].Value?.ToString() ?? "",
                                IsCorrect = (i == correctIndex)
                            });
                        }

                        _context.Questions.Add(question);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // 1. GET: Khởi tạo form với 1 câu hỏi rỗng mặc định
        public IActionResult Create()
        {
            var model = new UnifiedCreateViewModel();
            // Mặc định thêm sẵn 1 câu hỏi để giao diện không bị trống
            model.Questions.Add(new QuestionItem());

            // Load Dropdown
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title");
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title");

            return View(model);
        }

        // 2. POST: Xử lý lưu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnifiedCreateViewModel model)
        {
            // --- BƯỚC 1: XỬ LÝ TẠO TÀI NGUYÊN MỚI (NẾU CÓ) ---

            // a. Tạo bài đọc mới
            if (!string.IsNullOrEmpty(model.NewReadingTitle) && !string.IsNullOrEmpty(model.NewReadingContent))
            {
                var p = new ReadingPassage { Title = model.NewReadingTitle, Content = model.NewReadingContent };
                _context.ReadingPassages.Add(p);
                await _context.SaveChangesAsync();
                model.ReadingPassageId = p.Id; // Gán ID mới
            }

            // b. Tạo bài nghe mới
            if (model.NewListeningFile != null)
            {
                var audioName = DateTime.Now.Ticks + Path.GetExtension(model.NewListeningFile.FileName);
                var path = Path.Combine(_environment.WebRootPath, "uploads", "audio");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                using (var stream = new FileStream(Path.Combine(path, audioName), FileMode.Create))
                {
                    await model.NewListeningFile.CopyToAsync(stream);
                }

                var r = new ListeningResource
                {
                    Title = model.NewListeningTitle ?? "Audio " + DateTime.Now,
                    AudioUrl = "/uploads/audio/" + audioName,
                    Transcript = model.NewListeningTranscript
                };
                _context.ListeningResources.Add(r);
                await _context.SaveChangesAsync();
                model.ListeningResourceId = r.Id; // Gán ID mới
            }

            // c. Upload ảnh chung (cho Speaking/Writing)
            string? uploadedImageUrl = null;
            if (model.CommonImageFile != null)
            {
                var imgName = DateTime.Now.Ticks + Path.GetExtension(model.CommonImageFile.FileName);
                var path = Path.Combine(_environment.WebRootPath, "uploads", "images");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                using (var stream = new FileStream(Path.Combine(path, imgName), FileMode.Create))
                {
                    await model.CommonImageFile.CopyToAsync(stream);
                }
                uploadedImageUrl = "/uploads/images/" + imgName;
            }

            // --- BƯỚC 2: LƯU DANH SÁCH CÂU HỎI ---

            if (model.Questions != null && model.Questions.Count > 0)
            {
                foreach (var item in model.Questions)
                {
                    if (string.IsNullOrWhiteSpace(item.Content)) continue; // Bỏ qua dòng trống

                    var q = new Question
                    {
                        Content = item.Content,
                        Explaination = item.Explaination,
                        SkillType = model.SkillType,
                        Level = model.Level, // Lấy độ khó chung (hoặc item.Level nếu muốn riêng)
                        ReadingPassageId = (model.SkillType == ExamSkill.Reading) ? model.ReadingPassageId : null,
                        ListeningResourceId = (model.SkillType == ExamSkill.Listening) ? model.ListeningResourceId : null,
                        MediaUrl = uploadedImageUrl, // Gán ảnh chung
                        CreatedDate = DateTime.Now,
                        Answers = new List<Answer>()
                    };

                    // Chỉ tạo đáp án nếu KHÔNG phải Speaking/Writing
                    if (model.SkillType != ExamSkill.Speaking && model.SkillType != ExamSkill.Writing)
                    {
                        q.Answers.Add(new Answer { Content = item.AnswerA ?? "", IsCorrect = (item.CorrectAnswerIndex == 0) });
                        q.Answers.Add(new Answer { Content = item.AnswerB ?? "", IsCorrect = (item.CorrectAnswerIndex == 1) });
                        q.Answers.Add(new Answer { Content = item.AnswerC ?? "", IsCorrect = (item.CorrectAnswerIndex == 2) });
                        q.Answers.Add(new Answer { Content = item.AnswerD ?? "", IsCorrect = (item.CorrectAnswerIndex == 3) });
                    }

                    _context.Questions.Add(q);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        
        // EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Answers) // <--- BẮT BUỘC PHẢI CÓ DÒNG NÀY
                .Include(q => q.ReadingPassage)
                .Include(q => q.ListeningResource)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (question == null) return NotFound();

            // KIỂM TRA DỮ LIỆU: Nếu câu hỏi cũ chưa có đáp án (hoặc < 4), ta tạo bù vào cho đủ 4
            // Để tránh lỗi null khi hiển thị ra View
            if (question.Answers == null) question.Answers = new List<Answer>();

            while (question.Answers.Count < 4)
            {
                question.Answers.Add(new Answer { Content = "", IsCorrect = false });
            }

            // Load Dropdown như cũ
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", question.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", question.ListeningResourceId);

            return View(question);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question, int CorrectAnswerIndex, IFormFile? imageFile)
        {
            if (id != question.Id) return NotFound();
            ModelState.Remove("MediaUrl"); // Bỏ qua validate

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy dữ liệu cũ để giữ lại MediaUrl nếu không up ảnh mới
                    var oldQuestion = await _context.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Upload ảnh mới
                        var fileName = DateTime.Now.Ticks + Path.GetExtension(imageFile.FileName);
                        var path = Path.Combine(_environment.WebRootPath, "uploads", "images");
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        question.MediaUrl = "/uploads/images/" + fileName;
                    }
                    else
                    {
                        // Giữ ảnh cũ
                        question.MediaUrl = oldQuestion?.MediaUrl;
                    }
                    if (question.Answers != null)
                    {
                        var list = question.Answers.ToList();
                        for (int i = 0; i < list.Count; i++) list[i].IsCorrect = (i == CorrectAnswerIndex);
                    }

                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", question.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", question.ListeningResourceId);
            return View(question);
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions
                .Include(q => q.ReadingPassage)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null) return NotFound();
            return View(question);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id) => _context.Questions.Any(e => e.Id == id);

        [HttpGet]
        public async Task<IActionResult> GetResourceDetails(int id, string type)
        {
            if (type == "reading")
            {
                var item = await _context.ReadingPassages.FindAsync(id);
                if (item == null) return NotFound();
                // Trả về nội dung bài đọc
                return Json(new { success = true, content = item.Content });
            }
            else if (type == "listening")
            {
                var item = await _context.ListeningResources.FindAsync(id);
                if (item == null) return NotFound();
                // Trả về đường dẫn Audio và Transcript
                return Json(new { success = true, audioUrl = item.AudioUrl, transcript = item.Transcript });
            }
            return BadRequest();
        }
    }
}