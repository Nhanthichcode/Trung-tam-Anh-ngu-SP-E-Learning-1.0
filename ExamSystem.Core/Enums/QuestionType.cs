using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Core.Enums
{
    public enum QuestionType
    {
        [Display(Name = "Trắc nghiệm 1 đáp án")]
        SingleChoice = 1,   // (Radio button) Dùng cho Reading/Listening VSTEP

        [Display(Name = "Trắc nghiệm nhiều đáp án")]
        MultipleChoice = 2, // (Checkbox) Dùng cho IELTS (Pick 2 out of 5)

        [Display(Name = "Điền từ")]
        FillInTheBlank = 3, // (Input text) Dùng cho IELTS Listening/Reading

        [Display(Name = "Nối thông tin")]
        Matching = 4,       // (Drag & Drop) Dùng cho Matching Headings

        [Display(Name = "Tự luận")]
        Essay = 5,          // (Textarea) Dùng cho Writing Task 1 & 2

        [Display(Name = "Thu âm")]
        SpeakingRecording = 6, // (Record Button) Dùng cho Speaking

        [Display(Name = "Sắp xếp câu")]
        SentenceOrdering = 7   // (Optional)
    }
}