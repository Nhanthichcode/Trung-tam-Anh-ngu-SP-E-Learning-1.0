using ExamSystem.Core.Entities;
using System.Collections.Generic;

namespace ExamSystem.Web.Models
{
    public class ReadingDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string PassageText { get; set; }
        public int Level { get; set; }

        // Danh sách các câu hỏi thuộc bài đọc này
        public List<Question> Questions { get; set; } = new List<Question>();
    }
}