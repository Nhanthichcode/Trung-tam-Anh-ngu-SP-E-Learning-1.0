using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Entities
{
    public class AppUser : IdentityUser
    {
        // Thêm các trường riêng của dự án
        [Display(Name ="Học và tên")]
        public string? FullName { get; set; }
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        // Có thể thêm Avatar, Bio nếu cần
        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        // Navigation Properties (Quan hệ với các bảng khác)
        // Một User có thể làm nhiều bài thi (TestAttempts)
        // Chúng ta sẽ thêm vào sau khi tạo class TestAttempt
    }
}
