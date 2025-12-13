using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần cái này cho SelectList
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        // Đảm bảo có using ExamSystem.Web.Models;
        public async Task<IActionResult> Index(ExamSkill? skillType, int? level)
        {
            var query = _context.Questions
        .Include(q => q.ReadingPassage)
        .Include(q => q.ListeningResource)
        .OrderByDescending(q => q.CreatedDate)
        .AsQueryable();

            // 2. Áp dụng bộ lọc
            if (skillType.HasValue && skillType.Value != ExamSkill.None)
            {
                query = query.Where(q => q.SkillType == skillType.Value);
            }
            if (level.HasValue && level.Value > 0)
            {
                query = query.Where(q => q.Level == level.Value);
            }

            // 3. THỰC THI TRUY VẤN (Dòng bị thiếu trước đó)
            // Biến 'list' được tạo ra ở đây
            var list = await query.ToListAsync();

            var groupedList = new List<QuestionGroup>();

            // 1. Gom nhóm theo Bài Đọc (Reading)
            var readingGroups = list.Where(q => q.ReadingPassageId.HasValue)
                .GroupBy(q => q.ReadingPassageId)
                .Select(g => new QuestionGroup
                {
                    GroupType = "Reading",
                    GroupId = g.Key,
                    GroupTitle = g.First().ReadingPassage.Title, // Tiêu đề bài đọc
                    QuestionCount = g.Count(),
                    Questions = g.ToList()
                }).ToList();
            groupedList.AddRange(readingGroups);

            // 2. Gom nhóm theo Bài Nghe (Listening)
            var listeningGroups = list.Where(q => q.ListeningResourceId.HasValue && !q.ReadingPassageId.HasValue)
                .GroupBy(q => q.ListeningResourceId)
                .Select(g => new QuestionGroup
                {
                    GroupType = "Listening",
                    GroupId = g.Key,
                    GroupTitle = g.First().ListeningResource.Title, // Tiêu đề bài nghe
                    QuestionCount = g.Count(),
                    Questions = g.ToList()
                }).ToList();
            groupedList.AddRange(listeningGroups);

            // 3. Xử lý câu hỏi ĐỘC LẬP (Speaking, Writing, Grammar...)
            // THAY ĐỔI: Mỗi câu hỏi độc lập sẽ tạo thành 1 Group riêng để hiển thị thành 1 Card riêng
            var independentQuestions = list.Where(q => !q.ReadingPassageId.HasValue && !q.ListeningResourceId.HasValue).ToList();

            foreach (var q in independentQuestions)
            {
                groupedList.Add(new QuestionGroup
                {
                    GroupType = q.SkillType.ToString(), // "Speaking", "Writing"...
                    GroupId = null, // Không có bài gốc
                    GroupTitle = q.Content, // Tiêu đề nhóm chính là nội dung câu hỏi
                    QuestionCount = 1,
                    Questions = new List<Question> { q }
                });
            }

            // Sắp xếp lại: Reading/Listening lên trước, câu lẻ xuống dưới
            groupedList = groupedList
                .OrderByDescending(g => g.GroupType == "Reading" || g.GroupType == "Listening")
                .ThenByDescending(g => g.Questions.First().CreatedDate)
                .ToList();

            // Truyền kết quả sang View
            ViewData["CurrentSkill"] = skillType;
            ViewData["CurrentLevel"] = level;
            return View("Index", groupedList);
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
            // Bỏ qua validate các trường không cần thiết trong ModelState (do form động)
            ModelState.Remove("NewListeningFile");
            ModelState.Remove("CommonImageFile");
             if (model.SkillType == ExamSkill.Reading)
            {
                // Nếu có nhập Tiêu đề & Nội dung mới -> Tạo Bài Đọc mới
                if (!string.IsNullOrEmpty(model.NewReadingTitle) && !string.IsNullOrEmpty(model.NewReadingContent))
                {
                    var newPassage = new ReadingPassage
                    {
                        Title = model.NewReadingTitle,
                        Content = model.NewReadingContent
                    };
                    _context.ReadingPassages.Add(newPassage);
                    await _context.SaveChangesAsync(); // Lưu ngay để lấy ID

                     model.ReadingPassageId = newPassage.Id;
                }
            }

             if (model.SkillType == ExamSkill.Listening)
            {
                 if (model.NewListeningFile != null && model.NewListeningFile.Length > 0)
                {
                     var audioName = DateTime.Now.Ticks + Path.GetExtension(model.NewListeningFile.FileName);
                    var audioPath = Path.Combine(_environment.WebRootPath, "uploads", "audio");
                    if (!Directory.Exists(audioPath)) Directory.CreateDirectory(audioPath);

                    using (var stream = new FileStream(Path.Combine(audioPath, audioName), FileMode.Create))
                    {
                        await model.NewListeningFile.CopyToAsync(stream);
                    }

                     var newResource = new ListeningResource
                    {
                        Title = model.NewListeningTitle ?? "Audio - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        AudioUrl = "/uploads/audio/" + audioName,
                        Transcript = model.NewListeningTranscript
                    };
                    _context.ListeningResources.Add(newResource);
                    await _context.SaveChangesAsync(); // Lưu ngay để lấy ID

                     model.ListeningResourceId = newResource.Id;
                }
            }

             string? uploadedImageUrl = null;
            if ((model.SkillType == ExamSkill.Speaking || model.SkillType == ExamSkill.Writing) && model.CommonImageFile != null)
            {
                var imgName = DateTime.Now.Ticks + Path.GetExtension(model.CommonImageFile.FileName);
                var imgPath = Path.Combine(_environment.WebRootPath, "uploads", "images");
                if (!Directory.Exists(imgPath)) Directory.CreateDirectory(imgPath);

                using (var stream = new FileStream(Path.Combine(imgPath, imgName), FileMode.Create))
                {
                    await model.CommonImageFile.CopyToAsync(stream);
                }
                uploadedImageUrl = "/uploads/images/" + imgName;
            }

            // =========================================================================
            // 2. LƯU DANH SÁCH CÂU HỎI
            // =========================================================================

            if (model.Questions != null && model.Questions.Count > 0)
            {
                foreach (var item in model.Questions)
                {
                    // Bỏ qua các dòng trống (nếu người dùng lỡ bấm Thêm nhiều lần mà không nhập)
                    if (string.IsNullOrWhiteSpace(item.Content)) continue;

                    var q = new Question
                    {
                        Content = item.Content,
                        Explaination = item.Explaination,
                        SkillType = model.SkillType,
                        Level = model.Level, // Lấy độ khó chung đã chọn ở trên
                        CreatedDate = DateTime.Now,

                        // Gán Tài nguyên (ID đã chọn Cũ hoặc ID Mới vừa tạo ở bước 1)
                        ReadingPassageId = (model.SkillType == ExamSkill.Reading) ? model.ReadingPassageId : null,
                        ListeningResourceId = (model.SkillType == ExamSkill.Listening) ? model.ListeningResourceId : null,

                        // Gán ảnh (Nếu là Speaking/Writing)
                        MediaUrl = uploadedImageUrl
                    };

                     q.Answers = new List<Answer>();

                     bool isMultipleChoice = (model.SkillType != ExamSkill.Speaking && model.SkillType != ExamSkill.Writing);

                    if (isMultipleChoice)
                    {
                        q.Answers.Add(new Answer { Content = item.AnswerA ?? "", IsCorrect = (item.CorrectAnswerIndex == 0) });
                        q.Answers.Add(new Answer { Content = item.AnswerB ?? "", IsCorrect = (item.CorrectAnswerIndex == 1) });
                        q.Answers.Add(new Answer { Content = item.AnswerC ?? "", IsCorrect = (item.CorrectAnswerIndex == 2) });
                        q.Answers.Add(new Answer { Content = item.AnswerD ?? "", IsCorrect = (item.CorrectAnswerIndex == 3) });
                    }

                    _context.Questions.Add(q);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng nhập ít nhất 1 câu hỏi.");
            }
 
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", model.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", model.ListeningResourceId);

            return View(model);
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

        // POST : Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question, int CorrectAnswerIndex, IFormFile? imageFile)
        {
            if (id != question.Id) return NotFound();

            // 1. Lấy dữ liệu thực từ Database (Kèm theo Answers để update đáp án đúng)
            // KHÔNG DÙNG AsNoTracking() ở đây để EF tự động theo dõi thay đổi
            var existingQuestion = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existingQuestion == null) return NotFound();

            // 2. Validate dữ liệu (Bỏ qua MediaUrl vì ta xử lý file riêng)
            ModelState.Remove("MediaUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    // --- A. Cập nhật các thông tin cơ bản ---
                    existingQuestion.Content = question.Content;
                    existingQuestion.Level = question.Level;
                    existingQuestion.QuestionType = question.QuestionType;
                    existingQuestion.SkillType = question.SkillType;
                    existingQuestion.ReadingPassageId = question.ReadingPassageId;
                    existingQuestion.ListeningResourceId = question.ListeningResourceId;
                    existingQuestion.Explaination = question.Explaination;

                    // --- B. Xử lý Ảnh (MediaUrl) ---
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

                        // Gán đường dẫn mới
                        existingQuestion.MediaUrl = "/uploads/images/" + fileName;
                    }
                    // Nếu không có ảnh mới (else), existingQuestion.MediaUrl vẫn giữ nguyên giá trị cũ -> Không cần làm gì

                    // --- C. Xử lý Đáp án (Answers) ---
                    // Cập nhật lại nội dung đáp án và trạng thái đúng sai
                    if (question.Answers != null && existingQuestion.Answers != null)
                    {
                        // Lưu ý: Cần đảm bảo thứ tự Answers từ form khớp với Database hoặc dùng ID để map
                        // Ở đây giả định danh sách trả về từ Form có cùng thứ tự/số lượng
                        var newAnswers = question.Answers.ToList();
                        var dbAnswers = existingQuestion.Answers.ToList();

                        for (int i = 0; i < dbAnswers.Count; i++)
                        {
                            // Update nội dung đáp án nếu người dùng có sửa
                            if (i < newAnswers.Count)
                            {
                                dbAnswers[i].Content = newAnswers[i].Content;
                            }

                            // Update đúng sai dựa trên radio button đã chọn
                            dbAnswers[i].IsCorrect = (i == CorrectAnswerIndex);
                        }
                    }

                    // --- D. Lưu thay đổi ---
                    // Không cần gọi _context.Update(), chỉ cần SaveChangesAsync vì existingQuestion đang được Tracking
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Nếu Model lỗi, load lại Dropdown để hiển thị lại View
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

        // --- THÊM VÀO QuestionsController.cs ---

        // 1. GET: Lấy dữ liệu bài đọc/nghe + câu hỏi con
        public async Task<IActionResult> BatchEdit(int id, string type)
        {
            var model = new BatchEditViewModel { ResourceId = id, ResourceType = type };

            List<Question> questions = new List<Question>();

            if (type == "Reading")
            {
                var passage = await _context.ReadingPassages
                    .Include(p => p.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (passage == null) return NotFound();

                model.Title = passage.Title;
                model.Content = passage.Content;
                questions = passage.Questions.ToList();
            }
            else if (type == "Listening")
            {
                var resource = await _context.ListeningResources
                    .Include(r => r.Questions).ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (resource == null) return NotFound();

                model.Title = resource.Title;
                model.Content = resource.Transcript; // Dùng Content để chứa Transcript
                model.CurrentAudioUrl = resource.AudioUrl;
                questions = resource.Questions.ToList();
            }

            // Map câu hỏi sang ViewModel
            foreach (var q in questions)
            {
                var qItem = new QuestionEditItem
                {
                    Id = q.Id,
                    Content = q.Content,
                    Level = q.Level,
                    Explaination = q.Explaination,
                    MediaUrl = q.MediaUrl
                };

                // Map đáp án (Nếu là trắc nghiệm)
                if (q.Answers != null && q.Answers.Count >= 4)
                {
                    var ans = q.Answers.ToList();
                    qItem.AnswerA = ans[0].Content;
                    qItem.AnswerB = ans[1].Content;
                    qItem.AnswerC = ans[2].Content;
                    qItem.AnswerD = ans[3].Content;

                    // Tìm đáp án đúng
                    for (int i = 0; i < 4; i++) if (ans[i].IsCorrect == true ) qItem.CorrectAnswerIndex = i;
                }

                model.Questions.Add(qItem);
            }

            return View(model);
        }

        // 2. POST: Lưu thay đổi hàng loạt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchEdit(BatchEditViewModel model)
        {
            // A. Cập nhật Resource (Bài đọc/Nghe)
            if (model.ResourceType == "Reading")
            {
                var p = await _context.ReadingPassages.FindAsync(model.ResourceId);
                if (p != null) { p.Title = model.Title; p.Content = model.Content; }
            }
            else if (model.ResourceType == "Listening")
            {
                var r = await _context.ListeningResources.FindAsync(model.ResourceId);
                if (r != null)
                {
                    r.Title = model.Title;
                    r.Transcript = model.Content;
                    // Xử lý upload audio mới nếu có
                    if (model.NewAudioFile != null)
                    {
                        var fName = DateTime.Now.Ticks + Path.GetExtension(model.NewAudioFile.FileName);
                        /* ...Code save file tương tự hàm Create... */
                        r.AudioUrl = "/uploads/audio/" + fName;
                    }
                }
            }

            // B. Xóa các câu hỏi bị user xóa trên giao diện
            if (!string.IsNullOrEmpty(model.DeletedQuestionIds))
            {
                var idsToDelete = model.DeletedQuestionIds.Split(',').Select(int.Parse).ToList();
                var questionsToDelete = _context.Questions.Where(q => idsToDelete.Contains(q.Id));
                _context.Questions.RemoveRange(questionsToDelete);
            }

            // C. Cập nhật / Thêm mới câu hỏi
            foreach (var item in model.Questions)
            {
                Question question;
                bool isNew = (item.Id == 0);

                if (isNew)
                {
                    question = new Question(); // Tạo mới
                                               // Gán FK
                    if (model.ResourceType == "Reading") question.ReadingPassageId = model.ResourceId;
                    else question.ListeningResourceId = model.ResourceId;

                    question.SkillType = (model.ResourceType == "Reading") ? ExamSkill.Reading : ExamSkill.Listening;
                    question.CreatedDate = DateTime.Now;
                    _context.Questions.Add(question);
                }
                else
                {
                    question = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == item.Id);
                }

                if (question != null)
                {
                    question.Content = item.Content;
                    question.Level = item.Level;
                    question.Explaination = item.Explaination;

                    // Cập nhật đáp án
                    if (question.Answers == null) question.Answers = new List<Answer>();

                    // Nếu là mới, tạo 4 đáp án rỗng
                    if (isNew || question.Answers.Count < 4)
                    {
                        question.Answers.Clear();
                        for (int i = 0; i < 4; i++) question.Answers.Add(new Answer());
                    }

                    var ansList = question.Answers.ToList();
                    ansList[0].Content = item.AnswerA; ansList[0].IsCorrect = (item.CorrectAnswerIndex == 0);
                    ansList[1].Content = item.AnswerB; ansList[1].IsCorrect = (item.CorrectAnswerIndex == 1);
                    ansList[2].Content = item.AnswerC; ansList[2].IsCorrect = (item.CorrectAnswerIndex == 2);
                    ansList[3].Content = item.AnswerD; ansList[3].IsCorrect = (item.CorrectAnswerIndex == 3);
                }
            }

            await _context.SaveChangesAsync();
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
        [HttpGet]
        public async Task<IActionResult> GetAvailableQuestions(int examId, string type, int? skillType)
        {
            // 1. LẤY DANH SÁCH ID CÂU HỎI ĐÃ CÓ TRONG ĐỀ THI NÀY
            // Để kiểm tra xem câu hỏi nào đã được sử dụng
            var usedQuestionIds = await _context.ExamQuestions
                .Where(eq => eq.ExamPart.ExamId == examId)
                .Select(eq => eq.QuestionId)
                .ToListAsync();

            // 2. XỬ LÝ LỌC DỮ LIỆU
            if (type == "Independent") // Câu hỏi lẻ (Writing, Speaking, Grammar...)
            {
                // Chỉ lấy câu hỏi lẻ (không thuộc bài đọc/nghe)
                var query = _context.Questions.AsNoTracking()
                    .Where(q => q.ReadingPassageId == null && q.ListeningResourceId == null);

                // LỌC THEO SKILL TYPE (Nếu có yêu cầu)
                // Ví dụ: Nếu đang bấm nút "Thêm Đề Viết" -> chỉ hiện câu Writing
                if (skillType.HasValue)
                {
                    var skillEnum = (ExamSkill)skillType.Value;
                    query = query.Where(q => q.SkillType == skillEnum);
                }

                var data = await query
                    .Select(q => new
                    {
                        q.Id,
                        // Cắt ngắn nội dung nếu quá dài
                        Content = q.Content.Length > 100 ? q.Content.Substring(0, 100) + "..." : q.Content,
                        Skill = q.SkillType.ToString(),
                        q.Level,
                        // KIỂM TRA ĐÃ CHỌN CHƯA
                        IsSelected = usedQuestionIds.Contains(q.Id)
                    })
                    .ToListAsync();

                return Json(data);
            }
            else if (type == "Reading") // Bài Đọc
            {
                // Với Resource, ta kiểm tra xem có câu hỏi nào thuộc bài này đã nằm trong đề chưa
                var data = await _context.ReadingPassages.AsNoTracking()
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        QuestionCount = p.Questions.Count,
                        // Nếu bất kỳ câu hỏi nào của bài này đã nằm trong usedQuestionIds -> coi như bài này đã được chọn
                        IsSelected = p.Questions.Any(q => usedQuestionIds.Contains(q.Id))
                    })
                    .ToListAsync();
                return Json(data);
            }
            else if (type == "Listening") // Bài Nghe
            {
                var data = await _context.ListeningResources.AsNoTracking()
                    .Select(r => new
                    {
                        r.Id,
                        r.Title,
                        QuestionCount = r.Questions.Count,
                        IsSelected = r.Questions.Any(q => usedQuestionIds.Contains(q.Id))
                    })
                    .ToListAsync();
                return Json(data);
            }

            return BadRequest();
        }
    }
}