using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;

namespace ExamSystem.Web.Models
{
    public class UnifiedCreateViewModel
    {
        // 1. Cấu hình chung
        public ExamSkill SkillType { get; set; }
        public int Level { get; set; } = 1;

        // 2. Dành cho READING (Đọc)
        public int? ReadingPassageId { get; set; } // Chọn bài cũ
        public string? NewReadingTitle { get; set; } // Tạo bài mới
        public string? NewReadingContent { get; set; }

        // 3. Dành cho LISTENING (Nghe)
        public int? ListeningResourceId { get; set; } // Chọn bài cũ
        public string? NewListeningTitle { get; set; } // Tạo bài mới
        public IFormFile? NewListeningFile { get; set; }
        public string? NewListeningTranscript { get; set; }

        // 4. Dành cho SPEAKING / WRITING (Nói/Viết)
        // Thường dùng hình ảnh để mô tả (Describe image / Chart)
        public IFormFile? CommonImageFile { get; set; }

        // 5. Danh sách câu hỏi con
        public List<QuestionItem> Questions { get; set; } = new List<QuestionItem>();
    }

    public class QuestionItem
    {
        public string Content { get; set; }
        public string Explaination { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public string AnswerA { get; set; }
        public string AnswerB { get; set; }
        public string AnswerC { get; set; }
        public string AnswerD { get; set; }
    }
}