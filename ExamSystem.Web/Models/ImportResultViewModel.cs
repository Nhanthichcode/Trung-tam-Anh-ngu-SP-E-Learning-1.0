namespace ExamSystem.Web.Models
{
    public class ImportResultViewModel
    {
        public bool IsSuccess { get; set; } // Kiểm tra có thành công không
        public string Message { get; set; } // Thông báo chung
        public int ValidCount { get; set; } // Số dòng hợp lệ
        public int InvalidCount { get; set; } // Số dòng lỗi
        public List<ImportError> Errors { get; set; } = new List<ImportError>(); // Danh sách lỗi chi tiết
    }

    public class ImportError
    {
        public int Row { get; set; }
        public string ErrorMessage { get; set; }
    }
}
