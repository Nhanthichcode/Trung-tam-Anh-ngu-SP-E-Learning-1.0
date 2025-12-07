using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ExamSystem.Core.Enums;

namespace ExamSystem.Core.Entities
{
    public class Question
    {
        public int Id { get; set; }
        [Required] public string Content { get; set; }
        public QuestionType Type { get; set; }
        public int Level { get; set; }
        public string? MediaUrl { get; set; }

        public string? Explaination { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? ReadingPassageId { get; set; }
        public ReadingPassage? ReadingPassage { get; set; }
        public int? ListeningResourceId { get; set; }
        public ListeningResource? ListeningResource { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<QuestionTopic> QuestionTopics { get; set; } = new List<QuestionTopic>();
    }
}