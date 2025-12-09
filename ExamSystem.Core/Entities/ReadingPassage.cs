using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Entities
{
    public class ReadingPassage
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề bài đọc")]
        public string? Title { get; set; }

        [Required]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } // Đoạn văn dài

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
