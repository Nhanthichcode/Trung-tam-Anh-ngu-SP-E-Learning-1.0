using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class TestResult
    {
        public int Id { get; set; }
        [Display(Name = "Mã lần thi")]
        public int TestAttemptId { get; set; }
        public TestAttempt TestAttempt { get; set; }
        
        [Display(Name = "Mã bài thi")]
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        
        [Display(Name = "Đáp án đã chọn")]
        public string? SelectedAnswer { get; set; } // Lưu đáp án SV chọn (A, B, C...)
        [Display(Name = "Kết quả")]
        public bool IsCorrect { get; set; } // Đúng hay sai?
    }
}
