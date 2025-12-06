using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace ExamSystem.Web.Services
{
    // Class này giả vờ gửi email (thực tế không làm gì cả để code chạy tiếp)
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Ở đây sau này bạn có thể tích hợp SendGrid hoặc SMTP
            // Hiện tại cứ return về task hoàn thành để không bị lỗi
            return Task.CompletedTask;
        }
    }
}