using ExamSystem.Core.Enums;
using System.Collections.Generic;

namespace ExamSystem.Core.Entities
{
    // Bảng cha: Định nghĩa tên loại đề (VSTEP, IELTS, TOEIC...)
    public class ExamStructure
    {
        public int Id { get; set; }
        public string Name { get; set; } // VD: "VSTEP Bậc 3-5", "IELTS Academic"
        public string Description { get; set; }

        // Danh sách các phần mặc định của cấu trúc này
        public ICollection<StructurePart> Parts { get; set; } = new List<StructurePart>();
    }

    // Bảng con: Định nghĩa các phần thi trong cấu trúc
    public class StructurePart
    {
        public int Id { get; set; }
        public string Name { get; set; } // VD: "Kỹ năng Nghe (Listening)"
        public int OrderIndex { get; set; } // Thứ tự: 1, 2, 3, 4
        public string Description { get; set; } // VD: "35 phút, 3 phần nhỏ"
        public ExamSkill SkillType { get; set; }
        public int ExamStructureId { get; set; }
        public ExamStructure ExamStructure { get; set; }
    }
}