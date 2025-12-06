using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Entities
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nội dung đáp án")]
        public string Content { get; set; }

        [Display(Name = "Là đáp án đúng")]
        public bool IsCorrect { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }
    }
}