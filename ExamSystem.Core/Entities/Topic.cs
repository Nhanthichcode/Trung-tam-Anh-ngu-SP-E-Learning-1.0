using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Entities
{
    public class Topic
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên chủ đề")]
        public string Name { get; set; } // Ví dụ: Environment, Technology

        public string? Description { get; set; }

        // Quan hệ nhiều-nhiều với Question
        public ICollection<QuestionTopic> QuestionTopics { get; set; } = new List<QuestionTopic>();
    }
}