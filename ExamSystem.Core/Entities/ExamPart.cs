using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class ExamPart
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public int OrderIndex { get; set; }

        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; } = null!;

        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    }
}
