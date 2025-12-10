using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Core.Entities;

namespace ExamSystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestProcessController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestProcessController(AppDbContext context) => _context = context;

        // API: Nộp bài thi
        [HttpPost("submit-test")]
        public async Task<IActionResult> SubmitTest([FromBody] SubmissionModel model)
        {
            // 1. Tìm lượt thi
            var attempt = await _context.TestAttempts.FindAsync(model.TestAttemptId);
            if (attempt == null) return NotFound("Không tìm thấy lượt thi.");

            attempt.SubmitTime = DateTime.Now;
            attempt.Status = 1; // 1 = Completed

            double totalScore = 0;
            var results = new List<TestResult>();

            // 2. Chấm điểm từng câu
            foreach (var item in model.Answers)
            {
                var question = await _context.Questions.FindAsync(item.QuestionId);
                var correctAnswer = await _context.Answers
                    .FirstOrDefaultAsync(a => a.QuestionId == item.QuestionId && a.IsCorrect == true);

                bool isCorrect = false;
                // Kiểm tra nếu User chọn đúng đáp án Correct
                if (correctAnswer != null && item.SelectedAnswerId == correctAnswer.Id)
                {
                    isCorrect = true;
                    totalScore += 1; // Giả sử mỗi câu 1 điểm
                }

                results.Add(new TestResult
                {
                    TestAttemptId = attempt.Id,
                    QuestionId = item.QuestionId,
                    SelectedAnswerId = item.SelectedAnswerId,
                    IsCorrect = isCorrect,
                    ScoreObtained = isCorrect ? 1 : 0
                });
            }

            // 3. Lưu kết quả
            attempt.Score = totalScore;
            _context.TestResults.AddRange(results);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Nộp bài thành công", Score = totalScore });
        }
    }

    // Class phụ để hứng dữ liệu JSON gửi lên
    public class SubmissionModel
    {
        public int TestAttemptId { get; set; }
        public List<UserAnswerModel> Answers { get; set; }
    }
    public class UserAnswerModel
    {
        public int QuestionId { get; set; }
        public int SelectedAnswerId { get; set; }
    }
}