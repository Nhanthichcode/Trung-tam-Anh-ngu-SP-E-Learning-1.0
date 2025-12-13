using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Entities
{
    public class ReadingPassage
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
