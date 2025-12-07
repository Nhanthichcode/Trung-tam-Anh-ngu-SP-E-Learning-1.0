using ExamSystem.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class QuestionViewModel
    {
        // --- 1. DỮ LIỆU CƠ BẢN CỦA CÂU HỎI ---
        public int Id { get; set; }
        public QuestionType Type { get; set; }
        public int Level { get; set; } = 1;
        [Display(Name = "Nội dung câu hỏi")]
        public string? MediaUrl { get; set; }
        public string? Content { get; set; } // Nội dung câu hỏi (hoặc đề bài)
        [Display(Name = "Nội dung bài đọc")]
        public string? PassageText { get; set; }

        // --- 2. TRẮC NGHIỆM ĐƠN LẺ ---
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explaination { get; set; }

        // --- 3. LIÊN KẾT DỮ LIỆU ---
        [Display(Name = "Chọn Bài đọc")]
        public int? ReadingPassageId { get; set; }

        [Display(Name = "Chọn File nghe")]
        public int? ListeningResourceId { get; set; }

        [Display(Name = "Chủ đề")]
        public List<int> SelectedTopicIds { get; set; } = new List<int>();

        // --- 4. UPLOAD FILE (Cho câu hỏi đơn) ---
        [Display(Name = "File đính kèm")]
        public IFormFile? FileUpload { get; set; }

        // --- 5. DANH SÁCH CÂU HỎI CON (Cho bài Đọc/Nghe) ---
        public List<SubQuestionInput> SubQuestions { get; set; } = new List<SubQuestionInput>();
    }

    // Class phụ để hứng dữ liệu câu hỏi con
    public class SubQuestionInput
    {
        public int? Id { get; set; }
        public string? Content { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explaination { get; set; }
    }
}