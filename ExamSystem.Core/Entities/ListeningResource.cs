using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Entities
{
    public class ListeningResource
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề")]
        public string? Title { get; set; }

        [Required]
        public string AudioUrl { get; set; }

        [Display(Name = "Lời thoại (Transcript)")]
        public string? Transcript { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}