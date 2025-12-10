using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class TestResult
    {
        public int Id { get; set; }

        public int TestAttemptId { get; set; }
        [ForeignKey("TestAttemptId")]
        public TestAttempt TestAttempt { get; set; } = null!;

        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;

        public int? SelectedAnswerId { get; set; }
        public string? TextAnswer { get; set; }
        public string? AudioAnswerUrl { get; set; }

        public bool? IsCorrect { get; set; }
        public double ScoreObtained { get; set; }
        public string? Feedback { get; set; }
    }
}
