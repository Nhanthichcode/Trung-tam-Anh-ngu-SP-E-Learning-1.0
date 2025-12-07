using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Enums
{
    public enum QuestionType
    {
        //[Display(Name = "Câu hỏi chung")]
        //General = 0,
        //[Display(Name="Nhiều đáp án")]
        //MultipleChoice = 1, // Trắc nghiệm ABCD
        [Display(Name = "Bài viết")]
        Writing = 2,          // Tự luận (Writing)
        [Display(Name = "Bài nghe")] 
        Listening = 3,      // Nghe chọn đáp án
        [Display(Name = "Bài nói")]
        Speaking = 4,       // Nói (thu âm)
        [Display(Name = "Bài đọc")]
        ReadingPassage = 5
    }
}