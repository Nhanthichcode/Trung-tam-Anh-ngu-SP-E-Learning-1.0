namespace ExamSystem.Web.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalExams { get; set; }
        public int PendingGrades { get; set; } // Số bài cần giáo viên chấm

        // Danh sách bài thi mới nộp
        public List<RecentAttempt> RecentAttempts { get; set; } = new List<RecentAttempt>();
    }

    public class RecentAttempt
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string ExamTitle { get; set; }
        public DateTime SubmitTime { get; set; }
        public double? Score { get; set; }
        public bool IsGraded { get; set; }
    }
}