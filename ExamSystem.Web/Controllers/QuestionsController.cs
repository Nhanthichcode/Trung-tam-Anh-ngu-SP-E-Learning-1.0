using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // ============================================================
        // 1. DANH SÁCH (INDEX)
        // ============================================================
        public async Task<IActionResult> Index(ExamSkill? skillType, int? level)
        {
            var query = _context.Questions
                .Include(q => q.ReadingPassage)
                .Include(q => q.ListeningResource)
                .OrderByDescending(q => q.CreatedDate)
                .AsQueryable();

            if (skillType.HasValue && skillType.Value != ExamSkill.None)
            {
                query = query.Where(q => q.SkillType == skillType.Value);
            }
            if (level.HasValue && level.Value > 0)
            {
                query = query.Where(q => q.Level == level.Value);
            }

            var list = await query.ToListAsync();
            var groupedList = new List<QuestionGroup>();

            // Nhóm Reading
            groupedList.AddRange(list.Where(q => q.ReadingPassageId.HasValue)
                .GroupBy(q => q.ReadingPassageId)
                .Select(g => new QuestionGroup
                {
                    GroupType = "Reading",
                    GroupId = g.Key,
                    GroupTitle = g.First().ReadingPassage.Title,
                    QuestionCount = g.Count(),
                    Questions = g.ToList()
                }));

            // Nhóm Listening
            groupedList.AddRange(list.Where(q => q.ListeningResourceId.HasValue && !q.ReadingPassageId.HasValue)
                .GroupBy(q => q.ListeningResourceId)
                .Select(g => new QuestionGroup
                {
                    GroupType = "Listening",
                    GroupId = g.Key,
                    GroupTitle = g.First().ListeningResource.Title,
                    QuestionCount = g.Count(),
                    Questions = g.ToList()
                }));

            // Nhóm Câu lẻ (Independent) - Mỗi câu 1 Group để hiển thị Card riêng
            foreach (var q in list.Where(q => !q.ReadingPassageId.HasValue && !q.ListeningResourceId.HasValue))
            {
                groupedList.Add(new QuestionGroup
                {
                    GroupType = q.SkillType.ToString(),
                    GroupTitle = q.Content,
                    QuestionCount = 1,
                    Questions = new List<Question> { q }
                });
            }
            ViewData["CurrentSkill"] = skillType ?? ExamSkill.None;
            ViewData["CurrentLevel"] = level ?? 0;

            // Sắp xếp ưu tiên Reading/Listening lên đầu
            return View(groupedList.OrderByDescending(g => g.GroupType == "Reading" || g.GroupType == "Listening")
                                   .ThenByDescending(g => g.Questions.FirstOrDefault()?.CreatedDate).ToList());
        }

        // ============================================================
        // 2. TẠO MỚI (CREATE)
        // ============================================================
        public IActionResult Create()
        {
            var model = new UnifiedCreateViewModel();
            model.Questions.Add(new QuestionItem()); // Dòng mặc định
            LoadDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnifiedCreateViewModel model)
        {
            // Bỏ qua validate file vì xử lý thủ công
            ModelState.Remove("NewListeningFile");
            ModelState.Remove("CommonImageFile");

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }

            // A. Xử lý Tạo Tài nguyên mới (Nếu có)
            if (model.SkillType == ExamSkill.Reading && !string.IsNullOrEmpty(model.NewReadingTitle))
            {
                var newPassage = new ReadingPassage { Title = model.NewReadingTitle, Content = model.NewReadingContent };
                _context.ReadingPassages.Add(newPassage);
                await _context.SaveChangesAsync();
                model.ReadingPassageId = newPassage.Id;
            }
            else if (model.SkillType == ExamSkill.Listening && model.NewListeningFile != null)
            {
                var audioUrl = await SaveFileAsync(model.NewListeningFile, "audio");
                var newResource = new ListeningResource
                {
                    Title = model.NewListeningTitle ?? "Audio " + DateTime.Now.Ticks,
                    AudioUrl = audioUrl,
                    Transcript = model.NewListeningTranscript
                };
                _context.ListeningResources.Add(newResource);
                await _context.SaveChangesAsync();
                model.ListeningResourceId = newResource.Id;
            }

            // B. Xử lý Ảnh chung (Cho Writing/Speaking)
            string? uploadedImageUrl = null;

            // CHỈ UPLOAD NẾU SKILL LÀ SPEAKING VÀ CÓ FILE ĐƯỢC CHỌN (KHÔNG BẮT BUỘC)
            if (model.SkillType == ExamSkill.Speaking)
            {
                // Chỉ chạy SaveFileAsync nếu người dùng thực sự chọn file
                if (model.CommonImageFile != null)
                {
                    uploadedImageUrl = await SaveFileAsync(model.CommonImageFile, "images");
                }
                // ELSE: uploadedImageUrl vẫn là null (Không cần ảnh)
            }

            // C. Lưu Câu hỏi
            if (model.Questions != null && model.Questions.Any())
            {
                var questionsToAdd = new List<Question>();

                foreach (var item in model.Questions)
                {
                    if (string.IsNullOrWhiteSpace(item.Content)) continue;

                    var q = new Question
                    {
                        Content = item.Content,
                        Explaination = item.Explaination,
                        SkillType = model.SkillType,
                        Level = model.Level,
                        CreatedDate = DateTime.Now,
                        ReadingPassageId = (model.SkillType == ExamSkill.Reading) ? model.ReadingPassageId : null,
                        ListeningResourceId = (model.SkillType == ExamSkill.Listening) ? model.ListeningResourceId : null,
                        MediaUrl = uploadedImageUrl,
                        Answers = new List<Answer>()
                    };

                    // Thêm đáp án nếu là trắc nghiệm
                    if (model.SkillType != ExamSkill.Speaking && model.SkillType != ExamSkill.Writing)
                    {
                        q.Answers.Add(new Answer { Content = item.AnswerA ?? "", IsCorrect = (item.CorrectAnswerIndex == 0) });
                        q.Answers.Add(new Answer { Content = item.AnswerB ?? "", IsCorrect = (item.CorrectAnswerIndex == 1) });
                        q.Answers.Add(new Answer { Content = item.AnswerC ?? "", IsCorrect = (item.CorrectAnswerIndex == 2) });
                        q.Answers.Add(new Answer { Content = item.AnswerD ?? "", IsCorrect = (item.CorrectAnswerIndex == 3) });
                    }

                    questionsToAdd.Add(q);
                }

                if (questionsToAdd.Any())
                {
                    _context.Questions.AddRange(questionsToAdd);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 3. CHỈNH SỬA (EDIT) - Cho câu hỏi lẻ
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (question == null) return NotFound();

            // Đảm bảo đủ 4 dòng đáp án để view không lỗi
            if (question.Answers == null) question.Answers = new List<Answer>();
            while (question.Answers.Count < 4) question.Answers.Add(new Answer());

            LoadDropdowns(question);
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question, int CorrectAnswerIndex, IFormFile? imageFile, string DeleteImageFlag)
        {
            if (id != question.Id) return NotFound();
            var existingQuestion = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
            var dbQuestion = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
            if (dbQuestion == null) return NotFound();

            ModelState.Remove("MediaUrl"); // Bỏ qua validate

            // A. Cập nhật thông tin chính
            dbQuestion.Content = question.Content;
            dbQuestion.Level = question.Level;
            dbQuestion.SkillType = question.SkillType;
            dbQuestion.ReadingPassageId = question.ReadingPassageId;
            dbQuestion.ListeningResourceId = question.ListeningResourceId;
            dbQuestion.Explaination = question.Explaination;

            // B. Xử lý Ảnh

            // Trường hợp 1: Người dùng bấm XÓA ẢNH CŨ
            if (DeleteImageFlag == "true")
            {
                if (!string.IsNullOrEmpty(existingQuestion.MediaUrl))
                {
                    // Xóa file vật lý
                    var oldPath = Path.Combine(_environment.WebRootPath, existingQuestion.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }
                existingQuestion.MediaUrl = null; // Set DB field về NULL
            }

            // Trường hợp 2: Người dùng UPLOAD ẢNH MỚI
            if (imageFile != null && imageFile.Length > 0)
            {
                // Nếu đang có ảnh cũ (và chưa bị xóa ở bước 1) -> Xóa ảnh cũ
                if (!string.IsNullOrEmpty(existingQuestion.MediaUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, existingQuestion.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // Upload ảnh mới và gán đường dẫn
                var fileName = DateTime.Now.Ticks + Path.GetExtension(imageFile.FileName);
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "images");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existingQuestion.MediaUrl = "/uploads/images/" + fileName;
            }

            // C. Cập nhật Đáp án
            if (question.Answers != null && dbQuestion.Answers != null)
            {
                var formAnswers = question.Answers.ToList();
                var dbAnswers = dbQuestion.Answers.ToList();

                for (int i = 0; i < dbAnswers.Count; i++)
                {
                    if (i < formAnswers.Count) dbAnswers[i].Content = formAnswers[i].Content;
                    dbAnswers[i].IsCorrect = (i == CorrectAnswerIndex);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 4. BATCH EDIT (Sửa hàng loạt cho Bài Đọc/Nghe)
        // ============================================================
        public async Task<IActionResult> BatchEdit(int id, string type)
        {
            var model = new BatchEditViewModel { ResourceId = id, ResourceType = type };
            List<Question> questions = new List<Question>();

            if (type == "Reading")
            {
                var p = await _context.ReadingPassages.Include(x => x.Questions).ThenInclude(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
                if (p == null) return NotFound();
                model.Title = p.Title; model.Content = p.Content; questions = p.Questions.ToList();
            }
            else
            {
                var r = await _context.ListeningResources.Include(x => x.Questions).ThenInclude(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
                if (r == null) return NotFound();
                model.Title = r.Title; model.Content = r.Transcript; model.CurrentAudioUrl = r.AudioUrl; questions = r.Questions.ToList();
            }

            // Map sang ViewModel
            foreach (var q in questions)
            {
                var item = new QuestionEditItem
                {
                    Id = q.Id,
                    Content = q.Content,
                    Level = q.Level,
                    Explaination = q.Explaination,
                    MediaUrl = q.MediaUrl
                };
                if (q.Answers.Count >= 4)
                {
                    var ans = q.Answers.ToList();
                    item.AnswerA = ans[0].Content; item.AnswerB = ans[1].Content;
                    item.AnswerC = ans[2].Content; item.AnswerD = ans[3].Content;
                    for (int i = 0; i < 4; i++) if (ans[i].IsCorrect== true) item.CorrectAnswerIndex = i;
                }
                model.Questions.Add(item);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchEdit(BatchEditViewModel model)
        {
            // 1. Cập nhật Resource
            if (model.ResourceType == "Reading")
            {
                var p = await _context.ReadingPassages.FindAsync(model.ResourceId);
                if (p != null) { p.Title = model.Title; p.Content = model.Content; }
            }
            else
            {
                var r = await _context.ListeningResources.FindAsync(model.ResourceId);
                if (r != null)
                {
                    r.Title = model.Title; r.Transcript = model.Content;
                    if (model.NewAudioFile != null) r.AudioUrl = await SaveFileAsync(model.NewAudioFile, "audio");
                }
            }

            // 2. Xóa câu hỏi bị user xóa
            if (!string.IsNullOrEmpty(model.DeletedQuestionIds))
            {
                var ids = model.DeletedQuestionIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
                var toDelete = _context.Questions.Where(q => ids.Contains(q.Id));
                _context.Questions.RemoveRange(toDelete);
            }

            // 3. Cập nhật / Thêm câu hỏi
            foreach (var item in model.Questions)
            {
                Question q;
                if (item.Id == 0) // Thêm mới
                {
                    q = new Question
                    {
                        CreatedDate = DateTime.Now,
                        SkillType = model.ResourceType == "Reading" ? ExamSkill.Reading : ExamSkill.Listening,
                        ReadingPassageId = model.ResourceType == "Reading" ? model.ResourceId : null,
                        ListeningResourceId = model.ResourceType == "Listening" ? model.ResourceId : null,
                        Answers = new List<Answer> { new(), new(), new(), new() }
                    };
                    _context.Questions.Add(q);
                }
                else // Sửa
                {
                    q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == item.Id);
                }

                if (q != null)
                {
                    q.Content = item.Content; q.Level = item.Level; q.Explaination = item.Explaination;

                    var ansList = q.Answers.ToList();
                    // Đảm bảo đủ 4 đáp án
                    while (ansList.Count < 4) { var a = new Answer(); q.Answers.Add(a); ansList.Add(a); }

                    ansList[0].Content = item.AnswerA; ansList[0].IsCorrect = (item.CorrectAnswerIndex == 0);
                    ansList[1].Content = item.AnswerB; ansList[1].IsCorrect = (item.CorrectAnswerIndex == 1);
                    ansList[2].Content = item.AnswerC; ansList[2].IsCorrect = (item.CorrectAnswerIndex == 2);
                    ansList[3].Content = item.AnswerD; ansList[3].IsCorrect = (item.CorrectAnswerIndex == 3);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 5. IMPORT EXCEL
        // ============================================================
        public IActionResult Import() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length <= 0) return View();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    var questionsToAdd = new List<Question>();

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var content = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(content)) continue;

                        var q = new Question
                        {
                            Content = content,
                            SkillType = (ExamSkill)int.Parse(worksheet.Cells[row, 2].Value?.ToString() ?? "1"),
                            Level = int.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? "1"),
                            CreatedDate = DateTime.Now,
                            Answers = new List<Answer>()
                        };

                        int correctIdx = int.Parse(worksheet.Cells[row, 8].Value?.ToString() ?? "1");
                        for (int i = 1; i <= 4; i++)
                        {
                            q.Answers.Add(new Answer { Content = worksheet.Cells[row, 3 + i].Value?.ToString() ?? "", IsCorrect = (i == correctIdx) });
                        }
                        questionsToAdd.Add(q);
                    }
                    _context.Questions.AddRange(questionsToAdd);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 6. CÁC HÀM HỖ TRỢ (HELPER)
        // ============================================================

        // Helper: Lưu file vào thư mục wwwroot
        private async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var fileName = DateTime.Now.Ticks + Path.GetExtension(file.FileName);
            var path = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{folderName}/{fileName}";
        }

        // Helper: Load Dropdown cho View
        private void LoadDropdowns(Question? q = null)
        {
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", q?.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", q?.ListeningResourceId);
        }

        // API: Lấy chi tiết Resource (Dùng cho AJAX ở trang Create)
        [HttpGet]
        public async Task<IActionResult> GetResourceDetails(int id, string type)
        {
            if (type == "reading")
            {
                var item = await _context.ReadingPassages.FindAsync(id);
                return item == null ? NotFound() : Json(new { success = true, content = item.Content });
            }
            if (type == "listening")
            {
                var item = await _context.ListeningResources.FindAsync(id);
                return item == null ? NotFound() : Json(new { success = true, audioUrl = item.AudioUrl, transcript = item.Transcript });
            }
            return BadRequest();
        }

        // API: Lấy danh sách câu hỏi khả dụng cho Exam (Dùng ở trang soạn đề thi)
        [HttpGet]
        public async Task<IActionResult> GetAvailableQuestions(int examId, string type, int? skillType)
        {
            var usedIds = await _context.ExamQuestions.Where(eq => eq.ExamPart.ExamId == examId).Select(eq => eq.QuestionId).ToListAsync();

            if (type == "Independent")
            {
                var query = _context.Questions.AsNoTracking().Where(q => q.ReadingPassageId == null && q.ListeningResourceId == null);
                if (skillType.HasValue) query = query.Where(q => q.SkillType == (ExamSkill)skillType.Value);

                var data = await query.Select(q => new {
                    q.Id,
                    Content = q.Content.Length > 100 ? q.Content.Substring(0, 100) + "..." : q.Content,
                    Skill = q.SkillType.ToString(),
                    q.Level,
                    IsSelected = usedIds.Contains(q.Id)
                }).ToListAsync();
                return Json(data);
            }
            // Logic cho Reading/Listening tương tự (Giữ nguyên như bạn đã viết)
            return BadRequest();
        }

        // Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var q = await _context.Questions.Include(q => q.ReadingPassage).FirstOrDefaultAsync(m => m.Id == id);
            return q == null ? NotFound() : View(q);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var q = await _context.Questions.FindAsync(id);
            if (q != null) { _context.Questions.Remove(q); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}