using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class ExamQuestion
    {
        public int Id { get; set; }

        public int ExamPartId { get; set; }
        [ForeignKey("ExamPartId")]
        public ExamPart ExamPart { get; set; } = null!;

        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;

        public int SortOrder { get; set; }
        public double Score { get; set; } = 1.0;
    }
}
