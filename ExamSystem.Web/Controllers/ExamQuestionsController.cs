using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamQuestionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ExamQuestionsController(AppDbContext context) => _context = context;

        // Thêm câu hỏi vào đề thi (quan trọng)
        [HttpPost]
        public async Task<ActionResult<ExamQuestion>> AddQuestionToExam(ExamQuestion examQuestion)
        {
            _context.ExamQuestions.Add(examQuestion);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetExamQuestion", new { id = examQuestion.Id }, examQuestion);
        }

        // Lấy chi tiết 1 dòng gán (thường để check)
        [HttpGet("{id}")]
        public async Task<ActionResult<ExamQuestion>> GetExamQuestion(int id)
        {
            var examQuestion = await _context.ExamQuestions
                .Include(eq => eq.Question) // Load nội dung câu hỏi
                .FirstOrDefaultAsync(eq => eq.Id == id);

            if (examQuestion == null) return NotFound();
            return examQuestion;
        }

        // Xóa câu hỏi khỏi đề thi
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveQuestionFromExam(int id)
        {
            var examQuestion = await _context.ExamQuestions.FindAsync(id);
            if (examQuestion == null) return NotFound();
            _context.ExamQuestions.Remove(examQuestion);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}