using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        #region 1. INDEX & DETAILS CƠ BẢN

        public async Task<IActionResult> Index(string searchString, QuestionType? filterType, int? filterLevel, int? filterTopicId)
        {
            var questionsQuery = _context.Questions
                    .Include(q => q.ReadingPassage)
                    .Include(q => q.ListeningResource)
                    .Include(q => q.Answers)
                    .Include(q => q.QuestionTopics)
                        .ThenInclude(qt => qt.Topic) // Load Topic Name
                    .AsQueryable();

            // --- LOGIC LỌC ---
            if (filterType.HasValue)
                questionsQuery = questionsQuery.Where(s => s.Type == filterType);

            if (filterLevel.HasValue)
                questionsQuery = questionsQuery.Where(s => s.Level == filterLevel); // Lọc theo độ khó

            if (filterTopicId.HasValue)
                questionsQuery = questionsQuery.Where(s => s.QuestionTopics.Any(qt => qt.TopicId == filterTopicId)); // Lọc theo Topic

            if (!string.IsNullOrEmpty(searchString))
                questionsQuery = questionsQuery.Where(s => s.Content.Contains(searchString));
            // --- END LOGIC LỌC ---

            // Gán lại giá trị cho View để Select box không bị reset
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentType"] = filterType;
            ViewData["CurrentLevel"] = filterLevel;
            ViewData["CurrentTopicId"] = filterTopicId; // Dùng cho SelectList

            // Chuẩn bị ViewBag cho các Dropdown
            await PrepareViewBag(filterTopicId, filterLevel);

            // FIX: Sắp xếp GIẢM DẦN theo ngày tạo
            return View(await questionsQuery.OrderByDescending(q => q.CreatedDate).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await PrepareViewBag();
            return View(new QuestionViewModel());
        }

        #endregion

        #region 2. CREATE (POST)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionViewModel model)
        {
            ModelState.Remove("SubQuestions");

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.FileUpload != null && !IsFileValid(model.FileUpload, out string fileError))
                    {
                        ModelState.AddModelError("FileUpload", fileError);
                        await PrepareViewBag();
                        return View(model);
                    }

                    var question = new Question
                    {
                        Type = model.Type,
                        Level = model.Level,
                        Content = model.Content ?? "",
                        Explaination = model.Explaination,
                        CreatedDate = DateTime.Now
                    };

                    if (model.Type != QuestionType.Writing && model.Type != QuestionType.Speaking)
                    {
                        question.Answers = CreateAnswersFromModel(model);
                    }

                    if (model.FileUpload != null && model.FileUpload.Length > 0)
                    {
                        question.MediaUrl = await UploadFile(model.FileUpload);
                    }

                    if (model.SelectedTopicIds != null)
                    {
                        foreach (var topicId in model.SelectedTopicIds)
                        {
                            question.QuestionTopics.Add(new QuestionTopic { TopicId = topicId });
                        }
                    }

                    _context.Add(question);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống không xác định: " + ex.Message);
                }
            }
            await PrepareViewBag();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroupQuestion(QuestionViewModel model)
        {
            ModelState.Remove("Content");
            ModelState.Remove("FileUpload");
            ModelState.Remove("PassageTitle");
            foreach (var key in ModelState.Keys.Where(k => k.Contains("Explaination"))) ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                try
                {
                    if (!model.ReadingPassageId.HasValue)
                    {
                        if (!string.IsNullOrEmpty(model.PassageTitle) && !string.IsNullOrEmpty(model.PassageText))
                        {
                            // Create NEW Passage
                            var newPassage = new ReadingPassage
                            {
                                Title = model.PassageTitle,
                                Content = model.PassageText
                            };
                            _context.ReadingPassages.Add(newPassage);
                            await _context.SaveChangesAsync();

                            model.ReadingPassageId = newPassage.Id; // Use new ID
                        }
                        else
                        {
                            ModelState.AddModelError("", "Vui lòng chọn bài đọc có sẵn hoặc nhập đủ Tiêu đề và Nội dung để tạo mới.");
                        }
                    }
                    if (model.Type == QuestionType.Listening && !model.ListeningResourceId.HasValue)
                    {
                        ModelState.AddModelError("", "Vui lòng chọn File nghe từ kho.");
                        await PrepareViewBag();
                        return View("Create", model);
                    }

                    foreach (var sub in model.SubQuestions)
                    {
                        if (string.IsNullOrEmpty(sub.Content)) continue;

                        var q = new Question
                        {
                            Type = model.Type,
                            Level = model.Level,
                            CreatedDate = DateTime.Now,
                            ReadingPassageId = model.ReadingPassageId,
                            ListeningResourceId = model.ListeningResourceId,
                            Content = sub.Content,
                            Explaination = sub.Explaination,
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

            await PrepareViewBag();
            return View("Create", model);
        }

        #endregion

        #region 3. EDIT CÂU HỎI ĐƠN & NHÓM
        // 9. DETAILS/EDIT READING
        public async Task<IActionResult> DetailsReading(int? id)
        {
            if (id == null) return NotFound();

            // Tìm câu hỏi gốc (để lấy thông tin bài đọc)
            var rootQuestion = await _context.Questions
                .Include(q => q.ReadingPassage)
                .FirstOrDefaultAsync(m => m.ReadingPassageId == id || m.Id == id); // Tìm linh hoạt

            // Logic lấy PassageId và Content
            int? passageId = rootQuestion?.ReadingPassageId ?? id; // Ưu tiên ID bài đọc
            var passage = await _context.ReadingPassages.FindAsync(passageId);

            if (passage == null) return NotFound();

            // Lấy danh sách câu hỏi con
            var siblings = await _context.Questions
                .Include(q => q.Answers)
                .Where(q => q.ReadingPassageId == passageId)
                .OrderBy(q => q.Id)
                .ToListAsync();

            var model = new ReadingDetailsViewModel
            {
                Id = passage.Id, // QUAN TRỌNG: Gán ID bài đọc vào đây
                Title = passage.Title,
                PassageText = passage.Content,
                Level = rootQuestion?.Level ?? 1,
                Questions = siblings
            };

            return View(model);
        }
        public async Task<IActionResult> EditReading(int? id)
        {
            if (id == null) return NotFound();

            // 1. Tìm Bài Đọc (ReadingPassage) dựa trên ID được gửi lên
            // Lưu ý: ID gửi lên ở đây là ID của ReadingPassage (từ bảng ReadingPassages), không phải ID Question
            // HOẶC nếu bạn đang truyền ID của 1 câu hỏi con, phải tìm ngược ra cha.
            // Giả sử logic Index của bạn truyền ID của ReadingPassage (vì đã gom nhóm):

            // Tìm danh sách câu hỏi thuộc bài đọc này
            var siblings = await _context.Questions
                .Include(q => q.Answers)
                .Where(q => q.ReadingPassageId == id) // Lấy tất cả câu hỏi có ReadingPassageId này
                .OrderBy(q => q.Id)
                .ToListAsync();

            // Tìm nội dung bài đọc
            var passage = await _context.ReadingPassages.FindAsync(id);
            if (passage == null && siblings.Any())
            {
                // Fallback: nếu id truyền vào là id câu hỏi con
                var firstQ = await _context.Questions.FindAsync(id);
                if (firstQ?.ReadingPassageId != null)
                {
                    passage = await _context.ReadingPassages.FindAsync(firstQ.ReadingPassageId);
                    siblings = await _context.Questions.Include(a => a.Answers).Where(q => q.ReadingPassageId == firstQ.ReadingPassageId).ToListAsync();
                }
            }

            if (passage == null) return NotFound();

            var model = new QuestionViewModel
            {
                // Quan trọng: Gán ReadingPassageId để khi Post về biết là bài nào
                ReadingPassageId = passage.Id,
                PassageText = passage.Content,
                Type = QuestionType.ReadingPassage,
                // Map câu hỏi con sang ViewModel
                SubQuestions = siblings.Select(s => new SubQuestionInput
                {
                    Id = s.Id,
                    Content = s.Content,
                    Explaination = s.Explaination,
                    OptionA = s.Answers.ElementAtOrDefault(0)?.Content,
                    OptionB = s.Answers.ElementAtOrDefault(1)?.Content,
                    OptionC = s.Answers.ElementAtOrDefault(2)?.Content,
                    OptionD = s.Answers.ElementAtOrDefault(3)?.Content,
                    CorrectAnswer = GetCorrectChar(s.Answers)
                }).ToList()
            };

            return View("EditReading", model);
        }
        private string GetCorrectChar(ICollection<Answer> answers)
        {
            var list = answers.ToList();
            if (list.Count > 0 && list[0].IsCorrect) return "A";
            if (list.Count > 1 && list[1].IsCorrect) return "B";
            if (list.Count > 2 && list[2].IsCorrect) return "C";
            if (list.Count > 3 && list[3].IsCorrect) return "D";
            return "";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReading(QuestionViewModel model)
        {
            ModelState.Remove("Content");
            ModelState.Remove("FileUpload");
            foreach (var key in ModelState.Keys.Where(k => k.Contains("Explaination"))) ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                try
                {
                    var firstId = model.SubQuestions.FirstOrDefault(x => x.Id.HasValue)?.Id;
                    int? currentPassageId = null;

                    if (firstId.HasValue)
                    {
                        var firstQ = await _context.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == firstId);
                        currentPassageId = firstQ?.ReadingPassageId;

                        if (currentPassageId == null && !string.IsNullOrEmpty(model.PassageText))
                        {
                            var newPassage = new ReadingPassage { Content = model.PassageText, Title = "Migrated Passage " + DateTime.Now.Ticks };
                            _context.ReadingPassages.Add(newPassage);
                            await _context.SaveChangesAsync();
                            currentPassageId = newPassage.Id;
                        }
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

                    if (currentPassageId.HasValue)
                    {
                        var oldQuestions = await _context.Questions.Where(q => q.ReadingPassageId == currentPassageId).ToListAsync();
                        var submittedIds = model.SubQuestions.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();
                        var toDelete = oldQuestions.Where(q => !submittedIds.Contains(q.Id)).ToList();
                        if (toDelete.Any()) _context.Questions.RemoveRange(toDelete);
                    }

                    foreach (var sub in model.SubQuestions)
                    {
                        if (sub.Id.HasValue && sub.Id > 0)
                        {
                            var existingQ = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == sub.Id);
                            if (existingQ != null)
                            {
                                existingQ.ReadingPassageId = currentPassageId;
                                existingQ.Level = model.Level;
                                existingQ.Content = sub.Content;
                                existingQ.Explaination = sub.Explaination;
                                _context.Answers.RemoveRange(existingQ.Answers);
                                existingQ.Answers = CreateAnswerList(sub);
                                _context.Update(existingQ);
                            }
                        }
                        else
                        {
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
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống không xác định: " + ex.Message);
                }
            }

            return View("EditReading", model);
        }

        #endregion

        #region 4. DETAILS/EDIT LISTENING

        public async Task<IActionResult> DetailsListening(int? id)
        {
            if (id == null) return NotFound();
            var rootQuestion = await _context.Questions.Include(q => q.ListeningResource).FirstOrDefaultAsync(m => m.Id == id);
            if (rootQuestion == null || rootQuestion.Type != QuestionType.Listening) return NotFound();
            var siblings = await _context.Questions.Include(q => q.Answers).Where(q => q.ListeningResourceId == rootQuestion.ListeningResourceId).ToListAsync();
            ViewBag.AudioUrl = rootQuestion.ListeningResource?.AudioUrl;
            ViewBag.Transcript = rootQuestion.ListeningResource?.Transcript;
            ViewBag.Title = rootQuestion.ListeningResource?.Title;
            ViewBag.Level = rootQuestion.Level;
            return View(siblings);
        }

        public async Task<IActionResult> EditListening(int? id)
        {
            if (id == null) return NotFound();
            var rootQuestion = await _context.Questions.Include(q => q.ListeningResource).FirstOrDefaultAsync(m => m.Id == id);
            if (rootQuestion == null || rootQuestion.Type != QuestionType.Listening) return NotFound();
            var siblings = await _context.Questions.Include(q => q.Answers).Where(q => q.ListeningResourceId == rootQuestion.ListeningResourceId).OrderBy(q => q.Id).ToListAsync();

            var model = new QuestionViewModel
            {
                Content = rootQuestion.ListeningResource?.Transcript,
                ListeningResourceId = rootQuestion.ListeningResourceId,
                Level = rootQuestion.Level,
                Type = QuestionType.Listening,
                SubQuestions = siblings.Select(s =>
                {
                    var ansList = s.Answers.ToList();
                    string correctChar = null;
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
                        OptionA = ansList.ElementAtOrDefault(0)?.Content,
                        OptionB = ansList.ElementAtOrDefault(1)?.Content,
                        OptionC = ansList.ElementAtOrDefault(2)?.Content,
                        OptionD = ansList.ElementAtOrDefault(3)?.Content,
                        CorrectAnswer = correctChar
                    };
                }).ToList()
            };

            ViewBag.AudioUrl = rootQuestion.ListeningResource?.AudioUrl;
            return View("EditListening", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListening(QuestionViewModel model)
        {
            ModelState.Remove("Topic");
            ModelState.Remove("FileUpload");

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ListeningResourceId.HasValue)
                    {
                        var listening = await _context.ListeningResources.FindAsync(model.ListeningResourceId);
                        if (listening != null)
                        {
                            listening.Transcript = model.Content;
                            _context.Update(listening);
                        }
                    }

                    var submittedIds = model.SubQuestions.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();
                    var oldQuestions = await _context.Questions.Where(q => q.ListeningResourceId == model.ListeningResourceId).ToListAsync();
                    var toDelete = oldQuestions.Where(q => !submittedIds.Contains(q.Id)).ToList();
                    if (toDelete.Any()) _context.Questions.RemoveRange(toDelete);

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
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống không xác định: " + ex.Message);
                }
            }
            return View("EditListening", model);
        }

        #endregion

        #region 5. DELETE & Helper Functions

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions.FirstOrDefaultAsync(m => m.Id == id);
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

        private async Task PrepareViewBag(int? currentTopicId = null, int? currentLevel = null)
        {
            // 1. Chuẩn bị Levels
            var levelItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 - Dễ" },
                new SelectListItem { Value = "2", Text = "2 - Trung bình" },
                new SelectListItem { Value = "3", Text = "3 - Khó" },
                new SelectListItem { Value = "4", Text = "4 - Khó+" },
                new SelectListItem { Value = "5", Text = "5 - Rất khó" }
            };

            // 2. Chuẩn bị Topics
            ViewData["Topics"] = new SelectList(await _context.Topics.ToListAsync(), "Id", "Name", currentTopicId);

            // 3. Gán Levels (Chọn giá trị Level hiện tại)
            ViewData["Levels"] = new SelectList(levelItems, "Value", "Text", currentLevel?.ToString());

            // 4. Chuẩn bị Enum QuestionType cho bộ lọc chính
            ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)).Cast<QuestionType>().Select(v => new SelectListItem
            {
                Text = v.ToString(), // Bạn có thể dùng DisplayAttribute để lấy tên đẹp hơn
                Value = ((int)v).ToString()
            }).ToList(), "Value", "Text", ViewData["CurrentType"]);

            // 5. Chuẩn bị Bài đọc/Bài nghe cho form Create (nếu cần)
            ViewData["Passages"] = new SelectList(await _context.ReadingPassages.ToListAsync(), "Id", "Title");
            ViewData["Listenings"] = new SelectList(await _context.ListeningResources.ToListAsync(), "Id", "Title");
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

        private async Task<string> UploadFile(IFormFile file)
        {
            try
            {
                string uploadsFolder = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!System.IO.Directory.Exists(uploadsFolder)) System.IO.Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = System.Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return "/uploads/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"FATAL LOG: [ASYNC I/O CRASH] Lỗi ghi file: {ex.Message}");
                throw new InvalidOperationException($"Không thể lưu tệp tin. Vui lòng kiểm tra quyền truy cập.", ex);
            }
        }

        private bool IsFileValid(IFormFile file, out string errorMessage)
        {
            long maxFileSize = 5242880;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".wav", ".mp3" };

            if (file.Length > maxFileSize)
            {
                errorMessage = "Tệp quá lớn. Vui lòng chọn tệp dưới 5 MB.";
                return false;
            }

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                errorMessage = $"Định dạng tệp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        [HttpGet]
        public async Task<IActionResult> GetPassageContent(int id) // Nên dùng int id vì AJAX gửi số nguyên
        {
            var passage = await _context.ReadingPassages.FindAsync(id);
            if (passage == null)
            {
                return Json(new { content = "Không tìm thấy Bài đọc." }); // Trả về lỗi rõ ràng
            }
            // Trả về nội dung dưới dạng JSON
            return Json(new { content = passage.Content });
        }

        [HttpGet]
        public async Task<IActionResult> GetListeningContent(int id) // Nên dùng int id
        {
            var resource = await _context.ListeningResources.FindAsync(id);
            if (resource == null)
            {
                return Json(new { transcript = "Không tìm thấy Transcript.", audioUrl = "" }); // Trả về lỗi rõ ràng
            }
            // Trả về transcript và audio URL dưới dạng JSON
            return Json(new
            {
                transcript = resource.Transcript,
                audioUrl = resource.AudioUrl
            });
        }
        #endregion

        // ... (Các phần code cũ giữ nguyên) ...

        #region 6. SPEAKING ACTIONS (MỚI)

        public async Task<IActionResult> DetailsSpeaking(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions
                .Include(q => q.QuestionTopics).ThenInclude(qt => qt.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null || question.Type != QuestionType.Speaking) return NotFound();
            return View(question);
        }

        public async Task<IActionResult> EditSpeaking(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions
                .Include(q => q.QuestionTopics)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null || question.Type != QuestionType.Speaking) return NotFound();

            var model = new QuestionViewModel
            {
                Id = question.Id,
                Type = QuestionType.Speaking,
                Level = question.Level,
                Content = question.Content,
                Explaination = question.Explaination,
                MediaUrl = question.MediaUrl,
                SelectedTopicIds = question.QuestionTopics.Select(qt => qt.TopicId).ToList()
            };

            await PrepareViewBag(); // Tải Topics/Levels
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpeaking(int id, QuestionViewModel model, IFormFile fileUpload)
        {
            if (id != model.Id) return NotFound();

            // Validate cơ bản
            ModelState.Remove("SubQuestions");

            if (ModelState.IsValid)
            {
                try
                {
                    var question = await _context.Questions.Include(q => q.QuestionTopics).FirstOrDefaultAsync(q => q.Id == id);
                    if (question == null) return NotFound();

                    // Cập nhật thông tin
                    question.Content = model.Content;
                    question.Level = model.Level;
                    question.Explaination = model.Explaination;

                    // Xử lý File (Ảnh/Audio)
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        question.MediaUrl = await UploadFile(fileUpload);
                    }

                    // Cập nhật Chủ đề (Topics)
                    question.QuestionTopics.Clear();
                    if (model.SelectedTopicIds != null)
                    {
                        foreach (var topicId in model.SelectedTopicIds)
                        {
                            question.QuestionTopics.Add(new QuestionTopic { QuestionId = id, TopicId = topicId });
                        }
                    }

                    _context.Update(question);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            await PrepareViewBag();
            return View(model);
        }

        #endregion

        #region 7. WRITING ACTIONS (MỚI)

        public async Task<IActionResult> DetailsWriting(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions
                .Include(q => q.QuestionTopics).ThenInclude(qt => qt.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null || question.Type != QuestionType.Writing) return NotFound();
            return View(question);
        }

        public async Task<IActionResult> EditWriting(int? id)
        {
            if (id == null) return NotFound();
            var question = await _context.Questions
                .Include(q => q.QuestionTopics)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null || question.Type != QuestionType.Writing) return NotFound();

            var model = new QuestionViewModel
            {
                Id = question.Id,
                Type = QuestionType.Writing,
                Level = question.Level,
                Content = question.Content,
                Explaination = question.Explaination,
                SelectedTopicIds = question.QuestionTopics.Select(qt => qt.TopicId).ToList()
            };

            await PrepareViewBag();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWriting(int id, QuestionViewModel model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove("SubQuestions");
            ModelState.Remove("FileUpload"); // Writing thường không cần file

            if (ModelState.IsValid)
            {
                var question = await _context.Questions.Include(q => q.QuestionTopics).FirstOrDefaultAsync(q => q.Id == id);
                if (question == null) return NotFound();

                question.Content = model.Content;
                question.Level = model.Level;
                question.Explaination = model.Explaination;

                question.QuestionTopics.Clear();
                if (model.SelectedTopicIds != null)
                {
                    foreach (var topicId in model.SelectedTopicIds)
                    {
                        question.QuestionTopics.Add(new QuestionTopic { QuestionId = id, TopicId = topicId });
                    }
                }

                _context.Update(question);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PrepareViewBag();
            return View(model);
        }

        #endregion
    }
}