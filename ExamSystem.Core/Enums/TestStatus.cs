using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSystem.Core.Enums
{
    public enum TestStatus
    {
        Started = 0,        // Đang làm
        Submitted = 1,      // Đã nộp (Chưa chấm xong phần tự luận)
        Graded = 2          // Đã chấm xong hoàn toàn
    }
}
