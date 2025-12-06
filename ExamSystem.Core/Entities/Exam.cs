using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên đề thi")]
        public string Title { get; set; } // Ví dụ: Thi giữa kỳ Tiếng Anh

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Thời gian làm bài (phút)")]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Ngày mở đề")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày đóng đề")]
        public DateTime? EndDate { get; set; } // Null = Không giới hạn

        public bool IsActive { get; set; } = true;

        // Danh sách các câu hỏi trong đề này
        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    }
}
