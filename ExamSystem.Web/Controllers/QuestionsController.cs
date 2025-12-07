using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public QuestionsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. INDEX: Load dữ liệu từ bảng mới
        public async Task<IActionResult> Index(string searchString, QuestionType? filterType)
        {
            var questionsQuery = _context.Questions
                                         .Include(q => q.ReadingPassage)      // Bảng mới
                                         .Include(q => q.ListeningResource)   // Bảng mới
                                         .Include(q => q.Answers)             // Bảng mới (để hiện số đáp án nếu cần)
                                         .AsQueryable();

            if (filterType.HasValue)
                questionsQuery = questionsQuery.Where(s => s.Type == filterType);

            if (!string.IsNullOrEmpty(searchString))
                questionsQuery = questionsQuery.Where(s => s.Content.Contains(searchString));

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentType"] = filterType;

            return View(await questionsQuery.OrderByDescending(q => q.Id).ToListAsync());
        }

        // 2. DETAILS: Xem chi tiết câu hỏi đơn
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                                         .Include(q => q.Answers) // Load bảng Answers
                                         .Include(q => q.ListeningResource)
                                         .FirstOrDefaultAsync(m => m.Id == id);

            if (question == null) return NotFound();

            return View(question);
        }

        // 3. DETAILS READING: Xem chi tiết bài đọc
        public async Task<IActionResult> DetailsReading(int? id)
        {
            if (id == null) return NotFound();

            var rootQuestion = await _context.Questions
                                             .Include(q => q.ReadingPassage)
                                             .FirstOrDefaultAsync(m => m.Id == id);

            if (rootQuestion == null || rootQuestion.Type != QuestionType.ReadingPassage)
                return NotFound();

            // Lấy nội dung từ bảng ReadingPassage (ưu tiên) hoặc cột cũ
            string passageContent = rootQuestion.ReadingPassage?.Content ?? rootQuestion.PassageText;

            // Tìm các câu hỏi con
            var siblings = await _context.Questions
                                         .Include(q => q.Answers) // Load đáp án
                                         .Where(q => q.Type == QuestionType.ReadingPassage
                                                  && (q.ReadingPassageId == rootQuestion.ReadingPassageId))
                                         .ToListAsync();

            var model = new ReadingDetailsViewModel
            {
                PassageText = passageContent,
                Level = rootQuestion.Level,
                Questions = siblings
            };

            return View(model);
        }

        // 4. CREATE (GET)
        public IActionResult Create()
        {

            Console.WriteLine("--- BẮT ĐẦU chuẩn bị dữ liệu cho CREATE CÂU HỎI ĐƠN (POST) ---");
            PrepareViewBag();
            return View(new QuestionViewModel());
        }

        // 4. TẠO MỚI CÂU HỎI ĐƠN (POST) - Đã thêm try-catch để bắt lỗi crash
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionViewModel model)
        {
            Console.WriteLine("--- BẮT ĐẦU CREATE CÂU HỎI ĐƠN (POST) ---");
            ModelState.Remove("SubQuestions");

            if (ModelState.IsValid)
            {
                try
                {
                    var question = new Question
                    {
                        Type = model.Type,
                        Level = model.Level,
                        Content = model.Content ?? "",
                        Explaination = model.Explaination,
                        CreatedDate = DateTime.Now
                    };

                    // CHỈ LƯU ANSWERS NẾU LÀ TRẮC NGHIỆM (Type khác Writing/Speaking)
                    if (model.Type != QuestionType.Writing && model.Type != QuestionType.Speaking)
                    {
                        question.Answers = CreateAnswersFromModel(model);
                    }

                    // Xử lý File Upload (SPEAKING/LISTENING lẻ)
                    if (model.FileUpload != null && model.FileUpload.Length > 0)
                    {
                        Console.WriteLine("Đang upload file...");
                        question.MediaUrl = await UploadFile(model.FileUpload);
                        Console.WriteLine($"Đã lưu MediaUrl: {question.MediaUrl}");
                    }

                    // Xử lý Chủ đề
                    if (model.SelectedTopicIds != null)
                    {
                        foreach (var topicId in model.SelectedTopicIds)
                        {
                            question.QuestionTopics.Add(new QuestionTopic { TopicId = topicId });
                        }
                    }

                    _context.Add(question);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"THÀNH CÔNG: Đã lưu câu hỏi ID: {question.Id}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LỖI GHI DB: {ex.Message} - Stack: {ex.StackTrace}");
                    ModelState.AddModelError("", "Lỗi hệ thống không xác định: " + ex.Message);
                }
            }

            Console.WriteLine("THẤT BẠI: Trả về View do ModelState Invalid.");
            PrepareViewBag();
            return View(model);
        }

        // 6. CREATE GROUP (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroupQuestion(QuestionViewModel model)
        {
            ModelState.Remove("Content");
            ModelState.Remove("FileUpload");
            foreach (var key in ModelState.Keys.Where(k => k.Contains("Explaination"))) ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                try
                {
                    // A. TẠO BÀI ĐỌC MỚI (Nếu nhập text mới)
                    ReadingPassage newPassage = null;
                    if (model.Type == QuestionType.ReadingPassage && !string.IsNullOrEmpty(model.PassageText))
                    {
                        newPassage = new ReadingPassage
                        {
                            Content = model.PassageText,
                            Title = "New Passage " + DateTime.Now.Ticks
                        };
                        _context.ReadingPassages.Add(newPassage);
                        await _context.SaveChangesAsync();
                    }

                    // B. LƯU CÂU HỎI CON
                    foreach (var sub in model.SubQuestions)
                    {
                        if (string.IsNullOrEmpty(sub.Content)) continue;

                        var q = new Question
                        {
                            Type = model.Type,
                            Level = model.Level,
                            CreatedDate = DateTime.Now,

                            // Liên kết ID
                            ReadingPassageId = (newPassage != null) ? newPassage.Id : model.ReadingPassageId,
                            ListeningResourceId = model.ListeningResourceId,

                            Content = sub.Content,
                            Explaination = sub.Explaination,

                            // Tạo đáp án vào bảng Answers
                            Answers = CreateAnswerList(sub)
                        };

                        _context.Add(q);
                    }
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            PrepareViewBag();
            return View("Create", model);
        }

        // 7. EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                                         .Include(q => q.Answers) // Load đáp án để sửa
                                         .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            // Map ngược từ Entity sang ViewModel để hiển thị trên form
            var model = new QuestionViewModel
            {
                Id = question.Id,
                Type = question.Type,
                Level = question.Level,
                Content = question.Content,
                Explaination = question.Explaination,
                // Map Answers
                OptionA = question.Answers.FirstOrDefault(a => a.Content.StartsWith("A"))?.Content, // Logic tạm
                // ... (Bạn có thể map kỹ hơn nếu cần)
            };

            return View(question);
        }

        // 8. EDIT (POST) - Cập nhật bảng mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question questionInput, IFormFile fileUpload)
        {
            if (id != questionInput.Id) return NotFound();

            var questionInDb = await _context.Questions
                                             .Include(q => q.Answers)
                                             .FirstOrDefaultAsync(q => q.Id == id);

            if (questionInDb == null) return NotFound();

            ModelState.Remove("Topic");
            ModelState.Remove("fileUpload");

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật thông tin cơ bản
                    questionInDb.Content = questionInput.Content;
                    questionInDb.Level = questionInput.Level;
                    questionInDb.Explaination = questionInput.Explaination;

                    // Cập nhật File Upload
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        questionInDb.MediaUrl = await UploadFile(fileUpload);
                    }

                    // CẬP NHẬT ĐÁP ÁN (QUAN TRỌNG)
                    // Xóa đáp án cũ
                    _context.Answers.RemoveRange(questionInDb.Answers);

                    // Thêm đáp án mới từ Input (OptionA..D)
                    var newAnswers = new List<Answer>();
                    void AddAns(string content, string key)
                    {
                        if (!string.IsNullOrEmpty(content))
                            newAnswers.Add(new Answer { Content = content, IsCorrect = (questionInput.CorrectAnswer?.ToUpper() == key), QuestionId = id });
                    }
                    AddAns(questionInput.OptionA, "A");
                    AddAns(questionInput.OptionB, "B");
                    AddAns(questionInput.OptionC, "C");
                    AddAns(questionInput.OptionD, "D");

                    _context.Answers.AddRange(newAnswers);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Questions.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(questionInput);
        }

        // --- 9. EDIT READING (GET) - Lấy dữ liệu từ bảng mới ---
        public async Task<IActionResult> EditReading(int? id)
        {
            if (id == null) return NotFound();

            // 1. Tìm câu hỏi gốc để định vị bài đọc
            var rootQuestion = await _context.Questions
                                     .Include(q => q.ReadingPassage) // Lấy bài đọc từ bảng mới
                                     .FirstOrDefaultAsync(m => m.Id == id);

            if (rootQuestion == null || rootQuestion.Type != QuestionType.ReadingPassage)
                return NotFound();

            // 2. Xác định nội dung bài đọc (Ưu tiên bảng mới -> Cột cũ)
            string passageContent = rootQuestion.ReadingPassage?.Content ?? rootQuestion.PassageText;
            int? passageId = rootQuestion.ReadingPassageId;

            // 3. Tìm tất cả câu hỏi anh em
            // Logic: Nếu đã có PassageId thì tìm theo PassageId. Nếu chưa (dữ liệu cũ) thì tìm theo Text.
            List<Question> siblings;
            if (passageId.HasValue)
            {
                siblings = await _context.Questions
                                 .Include(q => q.Answers) // Load bảng Answers
                                 .Where(q => q.ReadingPassageId == passageId)
                                 .OrderBy(q => q.Id)
                                 .ToListAsync();
            }
            else
            {
                siblings = await _context.Questions
                                 .Include(q => q.Answers)
                                 .Where(q => q.Type == QuestionType.ReadingPassage
                                          && q.PassageText == rootQuestion.PassageText) // Fallback cũ
                                 .ToListAsync();
            }

            // 4. Map sang ViewModel
            var model = new QuestionViewModel
            {
                PassageText = passageContent,
                ReadingPassageId = passageId, // Lưu lại ID để update
                Level = rootQuestion.Level,
                SubQuestions = siblings.Select(s =>
                {
                    // Logic map Answers (List) -> OptionA/B/C/D (View)
                    // Giả định: Thứ tự lưu trong DB là A, B, C, D
                    var ansList = s.Answers.ToList();

                    // Xác định đáp án đúng (A, B, C hay D?)
                    string correctChar = s.CorrectAnswer; // Mặc định lấy cột cũ
                    if (ansList.Any())
                    {
                        if (ansList.Count > 0 && ansList[0].IsCorrect) correctChar = "A";
                        else if (ansList.Count > 1 && ansList[1].IsCorrect) correctChar = "B";
                        else if (ansList.Count > 2 && ansList[2].IsCorrect) correctChar = "C";
                        else if (ansList.Count > 3 && ansList[3].IsCorrect) correctChar = "D";
                    }

                    return new SubQuestionInput
                    {
                        Id = s.Id,
                        Content = s.Content,
                        Explaination = s.Explaination,
                        // Map 4 đáp án đầu tiên ra các ô nhập
                        OptionA = ansList.ElementAtOrDefault(0)?.Content ?? s.OptionA,
                        OptionB = ansList.ElementAtOrDefault(1)?.Content ?? s.OptionB,
                        OptionC = ansList.ElementAtOrDefault(2)?.Content ?? s.OptionC,
                        OptionD = ansList.ElementAtOrDefault(3)?.Content ?? s.OptionD,
                        CorrectAnswer = correctChar
                    };
                }).ToList()
            };

            return View("EditReading", model); // Trỏ về View EditReading
        }

        // --- 10. EDIT READING (POST) - Cập nhật vào bảng mới ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReading(QuestionViewModel model)
        {
            // Xóa validate thừa
            ModelState.Remove("Content");
            ModelState.Remove("FileUpload");
            foreach (var key in ModelState.Keys.Where(k => k.Contains("Explaination"))) ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                // A. TÌM BÀI ĐỌC GỐC ĐỂ CẬP NHẬT
                // Lấy ID của câu hỏi đầu tiên để truy ngược ra Bài đọc
                var firstId = model.SubQuestions.FirstOrDefault(x => x.Id.HasValue)?.Id;
                int? currentPassageId = null;

                if (firstId.HasValue)
                {
                    var firstQ = await _context.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == firstId);

                    // Logic xác định PassageId:
                    // 1. Lấy từ câu hỏi gốc trong DB
                    currentPassageId = firstQ?.ReadingPassageId;

                    // 2. Nếu trong DB chưa có (dữ liệu cũ), mà giờ người dùng sửa -> Tạo Passage mới luôn để chuẩn hóa
                    if (currentPassageId == null && !string.IsNullOrEmpty(model.PassageText))
                    {
                        var newPassage = new ReadingPassage
                        {
                            Content = model.PassageText,
                            Title = "Migrated Passage " + DateTime.Now.Ticks
                        };
                        _context.ReadingPassages.Add(newPassage);
                        await _context.SaveChangesAsync();
                        currentPassageId = newPassage.Id;
                    }
                    // 3. Nếu đã có ID -> Cập nhật nội dung bài đọc
                    else if (currentPassageId.HasValue)
                    {
                        var passage = await _context.ReadingPassages.FindAsync(currentPassageId);
                        if (passage != null)
                        {
                            passage.Content = model.PassageText;
                            _context.Update(passage);
                        }
                    }
                }

                // B. XỬ LÝ XÓA CÂU HỎI (DELETE)
                if (currentPassageId.HasValue)
                {
                    var oldQuestions = await _context.Questions
                                             .Where(q => q.ReadingPassageId == currentPassageId)
                                             .ToListAsync();

                    var submittedIds = model.SubQuestions.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();
                    var toDelete = oldQuestions.Where(q => !submittedIds.Contains(q.Id)).ToList();

                    if (toDelete.Any()) _context.Questions.RemoveRange(toDelete);
                }

                // C. XỬ LÝ THÊM / SỬA (UPSERT)
                foreach (var sub in model.SubQuestions)
                {
                    if (sub.Id.HasValue && sub.Id > 0)
                    {
                        // --- UPDATE ---
                        var existingQ = await _context.Questions
                                              .Include(q => q.Answers)
                                              .FirstOrDefaultAsync(q => q.Id == sub.Id);

                        if (existingQ != null)
                        {
                            existingQ.ReadingPassageId = currentPassageId; // Đảm bảo link đúng bài đọc
                            existingQ.Level = model.Level;
                            existingQ.Content = sub.Content;
                            existingQ.Explaination = sub.Explaination;

                            // Cập nhật bảng Answers (Xóa cũ -> Thêm mới)
                            _context.Answers.RemoveRange(existingQ.Answers);
                            existingQ.Answers = CreateAnswerList(sub); // Hàm helper đã có ở cuối file

                            _context.Update(existingQ);
                        }
                    }
                    else
                    {
                        // --- INSERT ---
                        var newQ = new Question
                        {
                            Type = QuestionType.ReadingPassage,
                            ReadingPassageId = currentPassageId,
                            Level = model.Level,
                            Content = sub.Content,
                            Explaination = sub.Explaination,
                            CreatedDate = DateTime.Now,
                            Answers = CreateAnswerList(sub)
                        };
                        _context.Add(newQ);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View("EditReading", model);
        }

        // --- HELPER METHODS ---
        private void PrepareViewBag()
        {
            ViewData["Topics"] = new MultiSelectList(_context.Topics, "Id", "Name");
            ViewData["Passages"] = new SelectList(_context.ReadingPassages, "Id", "Title");
            ViewData["Listenings"] = new SelectList(_context.ListeningResources, "Id", "Title");
        }

        private List<Answer> CreateAnswersFromModel(QuestionViewModel model)
        {
            var list = new List<Answer>();
            void Add(string c, string k) { if (!string.IsNullOrEmpty(c)) list.Add(new Answer { Content = c, IsCorrect = model.CorrectAnswer == k }); }
            Add(model.OptionA, "A"); Add(model.OptionB, "B"); Add(model.OptionC, "C"); Add(model.OptionD, "D");
            return list;
        }

        private List<Answer> CreateAnswerList(SubQuestionInput sub)
        {
            var list = new List<Answer>();
            void Add(string c, string k) { if (!string.IsNullOrEmpty(c)) list.Add(new Answer { Content = c, IsCorrect = sub.CorrectAnswer == k }); }
            Add(sub.OptionA, "A"); Add(sub.OptionB, "B"); Add(sub.OptionC, "C"); Add(sub.OptionD, "D");
            return list;
        }

        // Lưu ảnhp
        private async Task<string> UploadFile(IFormFile file)
        {
            // Giữ nguyên try-catch để bắt lỗi trong quá trình I/O
            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    // SỬ DỤNG COPYTOASYNC (Giống ArticlesController)
                    await file.CopyToAsync(fileStream);
                }

                return "/uploads/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                // Thông báo lỗi bất đồng bộ
                Console.WriteLine($"FATAL LOG: [ASYNC I/O CRASH] Lỗi ghi file: {ex.Message}");
                // Throw lại để hàm gọi bắt và trả về ModelState Error
                throw new InvalidOperationException($"Không thể lưu tệp tin. Vui lòng kiểm tra quyền truy cập.", ex);
            }
        }
        // GET: delete
        public async Task<IActionResult> Delete(int? id)
        {

            if (id == null) return NotFound();

            var question = await _context.Questions.FirstOrDefaultAsync(m => m.Id == id);

            if (question == null) return NotFound();

            return View(question);
        }


        // 9. XÓA (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)

        {

            var question = await _context.Questions.FindAsync(id);

            if (question != null)

            {

                // Tùy chọn: Xóa file vật lý nếu muốn tiết kiệm dung lượng

                //if (!string.IsNullOrEmpty(question.MediaUrl)) { ...System.IO.File.Delete... }

                _context.Questions.Remove(question);

                await _context.SaveChangesAsync();

            }

            return RedirectToAction(nameof(Index));
        }
        // --- 11. DETAILS LISTENING (GET) ---
        public async Task<IActionResult> DetailsListening(int? id)
        {
            if (id == null) return NotFound();

            var rootQuestion = await _context.Questions
                                     .Include(q => q.ListeningResource)
                                     .FirstOrDefaultAsync(m => m.Id == id);

            if (rootQuestion == null || rootQuestion.Type != QuestionType.Listening)
                return NotFound();

            var siblings = await _context.Questions
                                         .Include(q => q.Answers)
                                         .Where(q => q.Type == QuestionType.Listening
                                                  && q.ListeningResourceId == rootQuestion.ListeningResourceId)
                                         .ToListAsync();

            // Tận dụng ReadingDetailsViewModel hoặc tạo ViewModel mới
            // Ở đây tôi dùng ViewBag cho nhanh, hoặc bạn tạo ListeningDetailsViewModel tương tự
            ViewBag.AudioUrl = rootQuestion.ListeningResource?.AudioUrl;
            ViewBag.Transcript = rootQuestion.ListeningResource?.Transcript;
            ViewBag.Title = rootQuestion.ListeningResource?.Title;
            ViewBag.Level = rootQuestion.Level;

            return View(siblings);
        }

        // --- 12. EDIT LISTENING (GET) ---
        public async Task<IActionResult> EditListening(int? id)
        {
            if (id == null) return NotFound();

            var rootQuestion = await _context.Questions
                                     .Include(q => q.ListeningResource)
                                     .FirstOrDefaultAsync(m => m.Id == id);

            if (rootQuestion == null || rootQuestion.Type != QuestionType.Listening) return NotFound();

            // Tìm các câu hỏi con
            var siblings = await _context.Questions
                                 .Include(q => q.Answers)
                                 .Where(q => q.ListeningResourceId == rootQuestion.ListeningResourceId)
                                 .OrderBy(q => q.Id)
                                 .ToListAsync();

            // Map sang ViewModel
            var model = new QuestionViewModel
            {
                // Dùng Content để chứa Transcript tạm thời hiển thị
                Content = rootQuestion.ListeningResource?.Transcript,
                ListeningResourceId = rootQuestion.ListeningResourceId,
                Level = rootQuestion.Level,
                Type = QuestionType.Listening,

                // Map câu hỏi con
                SubQuestions = siblings.Select(s => new SubQuestionInput
                {
                    Id = s.Id,
                    Content = s.Content,
                    OptionA = s.Answers.ElementAtOrDefault(0)?.Content,
                    OptionB = s.Answers.ElementAtOrDefault(1)?.Content,
                    OptionC = s.Answers.ElementAtOrDefault(2)?.Content,
                    OptionD = s.Answers.ElementAtOrDefault(3)?.Content,
                    CorrectAnswer = s.Answers.FirstOrDefault(a => a.IsCorrect) != null
                                    ? (s.Answers.ToList().FindIndex(a => a.IsCorrect) == 0 ? "A" :
                                       s.Answers.ToList().FindIndex(a => a.IsCorrect) == 1 ? "B" :
                                       s.Answers.ToList().FindIndex(a => a.IsCorrect) == 2 ? "C" : "D")
                                    : ""
                }).ToList()
            };

            // Truyền thêm AudioUrl ra View để nghe thử
            ViewBag.AudioUrl = rootQuestion.ListeningResource?.AudioUrl;

            return View("EditListening", model);
        }

        // --- 13. EDIT LISTENING (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListening(QuestionViewModel model)
        {
            ModelState.Remove("Topic");
            ModelState.Remove("FileUpload"); // File audio xử lý riêng nếu cần

            if (ModelState.IsValid)
            {
                // A. CẬP NHẬT LISTENING RESOURCE (Nếu có sửa Transcript)
                if (model.ListeningResourceId.HasValue)
                {
                    var listening = await _context.ListeningResources.FindAsync(model.ListeningResourceId);
                    if (listening != null)
                    {
                        listening.Transcript = model.Content; // Lưu Transcript vào đây
                        _context.Update(listening);
                    }
                }

                // B. XỬ LÝ CÂU HỎI CON (Thêm/Sửa/Xóa)
                // Logic y hệt EditReading, chỉ thay ReadingPassageId bằng ListeningResourceId

                // 1. Xóa
                var submittedIds = model.SubQuestions.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();
                var oldQuestions = await _context.Questions
                                         .Where(q => q.ListeningResourceId == model.ListeningResourceId)
                                         .ToListAsync();
                var toDelete = oldQuestions.Where(q => !submittedIds.Contains(q.Id)).ToList();
                if (toDelete.Any()) _context.Questions.RemoveRange(toDelete);

                // 2. Upsert
                foreach (var sub in model.SubQuestions)
                {
                    if (sub.Id.HasValue && sub.Id > 0)
                    {
                        var existingQ = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == sub.Id);
                        if (existingQ != null)
                        {
                            existingQ.Content = sub.Content;
                            existingQ.Level = model.Level;
                            _context.Answers.RemoveRange(existingQ.Answers);
                            existingQ.Answers = CreateAnswerList(sub);
                            _context.Update(existingQ);
                        }
                    }
                    else
                    {
                        var newQ = new Question
                        {
                            Type = QuestionType.Listening,
                            ListeningResourceId = model.ListeningResourceId,
                            Level = model.Level,
                            Content = sub.Content,
                            CreatedDate = DateTime.Now,
                            Answers = CreateAnswerList(sub)
                        };
                        _context.Add(newQ);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("EditListening", model);
        }


        // KIỂM TRA FILE AN TOÀN TRƯỚC KHI LƯU
        private bool IsFileValid(IFormFile file, out string errorMessage)
        {
            long maxFileSize = 5242880; // 5 MB
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".wav", ".mp3" };

            if (file.Length > maxFileSize)
            {
                errorMessage = "Tệp quá lớn. Vui lòng chọn tệp dưới 5 MB.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                errorMessage = $"Định dạng tệp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}