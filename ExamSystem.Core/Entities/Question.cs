using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ExamSystem.Core.Enums;

namespace ExamSystem.Core.Entities
{
    public class Question
    {
        public int Id { get; set; }

        // --- GIỮ LẠI CÁC TRƯỜNG CŨ (LEGACY) ---
        [Required] public string Content { get; set; }
        public QuestionType Type { get; set; }
        public int Level { get; set; } // Sau này sẽ migrate sang thang 1-5

        // Các trường sắp xóa (đánh dấu Obsolete để nhớ)
        public string? MediaUrl { get; set; }
        public string? PassageText { get; set; }
        public string? Transcript { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explaination { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // --- CÁC QUAN HỆ MỚI (NEW SCHEMA) ---

        // 1. Liên kết bài đọc (Nullable)
        public int? ReadingPassageId { get; set; }
        public ReadingPassage? ReadingPassage { get; set; }

        // 2. Liên kết bài nghe (Nullable)
        public int? ListeningResourceId { get; set; }
        public ListeningResource? ListeningResource { get; set; }

        // 3. Danh sách đáp án mới (Thay thế Option A-D)
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();

        // 4. Danh sách chủ đề (1 câu nhiều chủ đề)
        public ICollection<QuestionTopic> QuestionTopics { get; set; } = new List<QuestionTopic>();
    }
}