using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    // [Authorize(Roles = "Admin" || "Teacher")]
    public class ExamsController : Controller
    {
        private readonly AppDbContext _context;

        public ExamsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH (INDEX)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams.OrderByDescending(e => e.StartDate).ToListAsync();
            return View(exams);
        }

        // ==========================================
        // 2. TẠO MỚI (CREATE)
        // ==========================================

        // 1. GET: Hiển thị form kèm danh sách cấu trúc mẫu
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách cấu trúc để hiển thị dropdown
            ViewBag.Structures = await _context.ExamStructures.ToListAsync();
            return View();
        }

        // 2. POST: Tạo đề thi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam exam, int? selectedStructureId)
        {
            if (ModelState.IsValid)
            {
                if (exam.StartDate == default) exam.StartDate = DateTime.Now;

                // Lưu Exam trước để có ID
                _context.Add(exam);
                await _context.SaveChangesAsync();

                // --- LOGIC MỚI: TỰ ĐỘNG TẠO PART THEO CẤU TRÚC ---
                if (selectedStructureId.HasValue)
                {
                    var structureParts = await _context.StructureParts
                        .Where(sp => sp.ExamStructureId == selectedStructureId)
                        .OrderBy(sp => sp.OrderIndex)
                        .ToListAsync();

                    if (structureParts.Any())
                    {
                        var newParts = new List<ExamPart>();
                        foreach (var sp in structureParts)
                        {
                            newParts.Add(new ExamPart
                            {
                                ExamId = exam.Id,
                                Name = sp.Name,        // Copy tên (VD: Kỹ năng Nghe)
                                OrderIndex = sp.OrderIndex, // Copy thứ tự
                                SkillType = sp.SkillType                           // Có thể thêm Description vào ExamPart nếu muốn
                            });
                        }
                        _context.ExamParts.AddRange(newParts);
                        await _context.SaveChangesAsync();
                    }
                }
                // --------------------------------------------------

                // Chuyển hướng thẳng đến trang soạn đề (Manage) thay vì Index
                return RedirectToAction("Manage", new { id = exam.Id });
            }

            ViewBag.Structures = await _context.ExamStructures.ToListAsync();
            return View(exam);
        }

        // ==========================================
        // 3. CHỈNH SỬA (EDIT)
        // ==========================================

        // GET: Hiển thị form sửa kèm dữ liệu cũ
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            return View(exam);
        }

        // POST: Lưu thay đổi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam exam)
        {
            if (id != exam.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamExists(exam.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // ==========================================
        // 4. XÓA (DELETE)
        // ==========================================

        // GET: Hiển thị trang xác nhận xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null) return NotFound();

            return View(exam);
        }

        // POST: Xóa thật sự
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra tồn tại
        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
        // GET: Exams/Manage/5 (Giao diện soạn đề)
        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
        .Include(e => e.ExamParts) // Lấy danh sách Parts
            .ThenInclude(ep => ep.ExamQuestions) // Lấy câu hỏi trong Part
                .ThenInclude(eq => eq.Question) // Lấy chi tiết câu hỏi
                    .ThenInclude(q => q.ReadingPassage) // Kèm bài đọc (nếu có)
        .Include(e => e.ExamParts)
            .ThenInclude(ep => ep.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.ListeningResource) // Kèm bài nghe (nếu có)
        .AsSplitQuery() // Tối ưu hiệu suất khi Include nhiều bảng con
        .FirstOrDefaultAsync(m => m.Id == id);

            if (exam == null) return NotFound();

            // Sắp xếp các phần thi theo thứ tự trước khi gửi sang View
            exam.ExamParts = exam.ExamParts.OrderBy(p => p.OrderIndex).ToList();

            return View(exam);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableQuestions(int examId, string type, int? skillType)
        {
            // 1. LẤY DANH SÁCH ID CÂU HỎI ĐÃ CÓ TRONG ĐỀ (Để đánh dấu IsSelected)
            var usedQuestionIds = await _context.ExamQuestions
                .Where(eq => eq.ExamPart.ExamId == examId)
                .Select(eq => eq.QuestionId)
                .ToListAsync();

            // 2. XỬ LÝ THEO TỪNG LOẠI
            if (type == "Independent") // A. CÂU HỎI LẺ (Writing, Speaking, Grammar, hoặc câu lẻ Reading/Listening)
            {
                // Bước 1: Lấy tất cả câu hỏi "mồ côi" (không thuộc bài đọc/nghe nào)
                var query = _context.Questions.AsNoTracking()
                    .Where(q => q.ReadingPassageId == null && q.ListeningResourceId == null);

                // Bước 2: LỌC NGHIÊM NGẶT THEO KỸ NĂNG (Đây là phần sửa lỗi)
                if (skillType.HasValue)
                {
                    var skillEnum = (ExamSkill)skillType.Value;
                    query = query.Where(q => q.SkillType == skillEnum);
                }

                var data = await query
                    .Select(q => new
                    {
                        q.Id,
                        // Cắt ngắn nội dung để hiển thị gọn
                        Content = q.Content.Length > 100 ? q.Content.Substring(0, 100) + "..." : q.Content,
                        Skill = q.SkillType.ToString(),
                        q.Level,
                        // Đánh dấu nếu đã có trong đề
                        IsSelected = usedQuestionIds.Contains(q.Id)
                    })
                    .ToListAsync();

                return Json(data);
            }
            else if (type == "Reading") // B. BÀI ĐỌC (PASSAGE)
            {
                var data = await _context.ReadingPassages.AsNoTracking()
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        QuestionCount = p.Questions.Count,
                        // Nếu bất kỳ câu nào của bài này đã có trong đề -> Đánh dấu đã chọn
                        IsSelected = p.Questions.Any(q => usedQuestionIds.Contains(q.Id))
                    })
                    .ToListAsync();
                return Json(data);
            }
            else if (type == "Listening") // C. BÀI NGHE (RESOURCE)
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
        // POST: Add Single Question
        [HttpPost]
        public async Task<IActionResult> AddSingleQuestion(int examPartId, int questionId, float score)
        {
            bool exists = await _context.ExamQuestions.AnyAsync(eq => eq.ExamPartId == examPartId && eq.QuestionId == questionId);
            if (!exists)
            {
                int maxOrder = await _context.ExamQuestions
                    .Where(eq => eq.ExamPartId == examPartId)
                    .MaxAsync(eq => (int?)eq.SortOrder) ?? 0;

                var eq = new ExamQuestion
                {
                    ExamPartId = examPartId,
                    QuestionId = questionId,
                    Score = score,
                    SortOrder = maxOrder + 1
                };
                _context.ExamQuestions.Add(eq);
                await _context.SaveChangesAsync();
            }
            var part = await _context.ExamParts.FindAsync(examPartId);
            return RedirectToAction("Manage", new { id = part.ExamId });
        }

        // POST: Add Resource Group (Reading/Listening)
        [HttpPost]
        public async Task<IActionResult> AddResourceGroup(int examPartId, int resourceId, string type, float scorePerQuestion)
        {
            List<Question> questionsToAdd = new List<Question>();

            if (type == "Reading")
            {
                questionsToAdd = await _context.Questions.Where(q => q.ReadingPassageId == resourceId).ToListAsync();
            }
            else if (type == "Listening")
            {
                questionsToAdd = await _context.Questions.Where(q => q.ListeningResourceId == resourceId).ToListAsync();
            }

            if (questionsToAdd.Any())
            {
                int maxOrder = await _context.ExamQuestions
                    .Where(eq => eq.ExamPartId == examPartId)
                    .MaxAsync(eq => (int?)eq.SortOrder) ?? 0;

                foreach (var q in questionsToAdd)
                {
                    bool exists = await _context.ExamQuestions.AnyAsync(eq => eq.ExamPartId == examPartId && eq.QuestionId == q.Id);
                    if (!exists)
                    {
                        maxOrder++;
                        _context.ExamQuestions.Add(new ExamQuestion
                        {
                            ExamPartId = examPartId,
                            QuestionId = q.Id,
                            Score = scorePerQuestion,
                            SortOrder = maxOrder
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            var part = await _context.ExamParts.FindAsync(examPartId);
            return RedirectToAction("Manage", new { id = part.ExamId });
        }

        // POST: Remove Question
        [HttpPost]
        public async Task<IActionResult> RemoveQuestionFromPart(int examId, int examQuestionId)
        {
            var link = await _context.ExamQuestions.FindAsync(examQuestionId);
            if (link != null)
            {
                _context.ExamQuestions.Remove(link);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Manage", new { id = examId });
        }

        [HttpPost]
        public async Task<IActionResult> QuickAddPart(int examId, string name, ExamSkill skill, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Tên phần thi không được để trống!" });
            }

            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return Json(new { success = false, message = "Không tìm thấy đề thi!" });

            // Tự động tính thứ tự tiếp theo
            var lastOrder = await _context.ExamParts
                .Where(p => p.ExamId == examId)
                .MaxAsync(p => (int?)p.OrderIndex) ?? 0;

            var newPart = new ExamPart
            {
                ExamId = examId,
                Name = name.Trim(),
                SkillType = skill, // Quan trọng để gom nhóm đúng
                OrderIndex = lastOrder + 1
            };

            _context.ExamParts.Add(newPart);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}