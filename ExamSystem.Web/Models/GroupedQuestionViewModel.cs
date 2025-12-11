using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;

namespace ExamSystem.Web.Models
{
    // Đại diện cho một nhóm câu hỏi (Bài đọc, Bài nghe, hoặc Câu hỏi độc lập)
    public class QuestionGroup
    {
        public string GroupType { get; set; } // Reading, Listening, Independent
        public string GroupTitle { get; set; } // Tiêu đề Bài đọc/Nghe
        public int? GroupId { get; set; } // ID của Bài đọc/Nghe (để Sửa/Xóa)
        public int QuestionCount { get; set; } // Số lượng câu hỏi trong nhóm
        public List<Question> Questions { get; set; } // Danh sách câu hỏi chi tiết
    }
}