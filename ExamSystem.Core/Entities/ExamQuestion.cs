using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class ExamQuestion
    {
        public int Id { get; set; }

        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }

        public int SortOrder { get; set; } // Thứ tự câu hỏi trong đề
        public double Score { get; set; } // Điểm số riêng cho câu hỏi trong đề này
    }
}
