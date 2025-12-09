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
        [Required] public string Content { get; set; }
        public QuestionType Type { get; set; }
        public int Level { get; set; }
        public string? MediaUrl { get; set; }

        public string? Explaination { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? ReadingPassageId { get; set; } // Cột lưu ID

        [ForeignKey("ReadingPassageId")] // <--- QUAN TRỌNG: Chỉ định cột khóa ngoại
        public virtual ReadingPassage ReadingPassage { get; set; } // Object liên kết

        // --- KHÓA NGOẠI CHO BÀI NGHE ---
        public int? ListeningResourceId { get; set; } // Cột lưu ID

        [ForeignKey("ListeningResourceId")] // <--- QUAN TRỌNG: Chỉ định cột khóa ngoại
        public virtual ListeningResource ListeningResource { get; set; } // Object liên kết


        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<QuestionTopic> QuestionTopics { get; set; } = new List<QuestionTopic>();
    }
}