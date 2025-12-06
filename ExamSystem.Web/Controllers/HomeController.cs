using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
