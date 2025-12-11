using ExamSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Core.Entities
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        // Phân loại
        public ExamSkill SkillType { get; set; }
        public QuestionType QuestionType { get; set; }
        public int Level { get; set; } = 1;

        public string? Explaination { get; set; }
        public string? MediaUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Liên kết (Không dùng [ForeignKey] để tránh lỗi ID1)
        public int? ListeningResourceId { get; set; }
        public ListeningResource? ListeningResource { get; set; }

        public int? ReadingPassageId { get; set; }
        public ReadingPassage? ReadingPassage { get; set; }

        public List<Answer> Answers { get; set; } = new List<Answer>();
    }
}