using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Core.Entities
{
    public class Answer
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public bool? IsCorrect { get; set; }
        public string? TextAnswer { get; set; }

        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
    }
}