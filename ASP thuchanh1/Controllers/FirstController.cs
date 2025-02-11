using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace ASP_thuchanh1.Controllers
{
    public class FirstController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Welcome(string HovaTen, int Tuoi = 1)
        {
            ViewData["Greeting"] = "Xin chao, toi la " + HovaTen + ". Tuoi cua toi la " + Tuoi.ToString() + ".";
            return View();
        }
    }
}
