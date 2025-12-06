using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class TestAttempt
    {
        public int Id { get; set; }

        [Display(Name = "Người thi")]
        public string UserId { get; set; }
        public AppUser User { get; set; }

        [Display(Name = "Đề thi")]
        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        [Display(Name = "Bắt đầu lúc")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [Display(Name = "Nộp bài lúc")]
        public DateTime? SubmitTime { get; set; }

        [Display(Name = "Điểm số")]
        public double Score { get; set; }

        public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}
