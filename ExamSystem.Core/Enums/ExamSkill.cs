using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ExamSystem.Core.Enums
{
    public enum ExamSkill
    {
        [Display(Name = "Khác")]
        None = 0,
        [Display(Name = "Nghe (Listening)")]
        Listening = 1,
        [Display(Name = "Đọc (Reading)")]
        Reading = 2,
        [Display(Name = "Viết (Writing)")]
        Writing = 3,
        [Display(Name = "Nói (Speaking)")]
        Speaking = 4,
        [Display(Name = "Ngữ pháp (Grammar)")]
        Grammar = 5
    }
}
