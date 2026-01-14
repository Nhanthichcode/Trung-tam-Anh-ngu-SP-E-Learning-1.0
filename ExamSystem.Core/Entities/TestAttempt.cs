using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class TestAttempt
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;

        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; } = null!;

        public DateTime StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public double Score { get; set; }
        public int Status { get; set; }
        public Boolean isGraded { get; set; } = false;
        public string? TeacherFeedback { get; set; }

        public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}
