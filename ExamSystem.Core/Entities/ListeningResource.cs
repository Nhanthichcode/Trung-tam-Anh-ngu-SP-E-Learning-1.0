using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Entities
{
    public class ListeningResource
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [Required]
        public string AudioUrl { get; set; } = string.Empty;
        public string? Transcript { get; set; }
    }
}