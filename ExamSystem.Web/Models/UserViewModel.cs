using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class UserViewModel
    {
        // Chỉ hiển thị, không cho sửa
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string Roles { get; set; }
        public bool IsLocked { get; set; }
    }
}