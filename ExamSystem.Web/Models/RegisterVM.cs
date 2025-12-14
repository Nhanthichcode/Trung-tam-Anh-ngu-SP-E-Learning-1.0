using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Nhập Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Nhập Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; }
    }
}