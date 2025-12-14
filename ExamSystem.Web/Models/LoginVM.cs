using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}