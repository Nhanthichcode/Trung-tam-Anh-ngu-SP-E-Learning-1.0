using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;

namespace ExamSystem.Web.Models
{
    public class BatchEditViewModel
    {
        // Thông tin chung
        public int ResourceId { get; set; } // ID của ReadingPassage hoặc ListeningResource
        public string ResourceType { get; set; } // "Reading" hoặc "Listening"

        // Dữ liệu Resource
        public string Title { get; set; }
        public string Content { get; set; } // Nội dung bài đọc hoặc Transcript bài nghe
        public string? CurrentAudioUrl { get; set; } // Link audio hiện tại
        public IFormFile? NewAudioFile { get; set; } // Upload audio mới (nếu muốn thay)

        // Danh sách câu hỏi
        public List<QuestionEditItem> Questions { get; set; } = new List<QuestionEditItem>();

        // Danh sách ID các câu hỏi cần xóa (được xử lý bởi JS)
        public string? DeletedQuestionIds { get; set; }
    }

    public class QuestionEditItem
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int Level { get; set; }
        public string? Explaination { get; set; }
        public string? MediaUrl { get; set; } // Ảnh của câu hỏi (nếu có)

        // Đáp án
        public int CorrectAnswerIndex { get; set; }
        public string AnswerA { get; set; }
        public string AnswerB { get; set; }
        public string AnswerC { get; set; }
        public string AnswerD { get; set; }
    }
}