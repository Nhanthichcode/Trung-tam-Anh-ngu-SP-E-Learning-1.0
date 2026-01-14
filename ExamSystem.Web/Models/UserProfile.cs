using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class UserProfileVM
    {
        // Chỉ hiển thị, không cho sửa
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Chọn ảnh đại diện")]
        public IFormFile? AvatarUpload { get; set; }
        public string? AvatarUrl { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; } // Cần thêm trường này vào AppUser nếu chưa có
    }
}