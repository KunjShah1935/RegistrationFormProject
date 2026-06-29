using Microsoft.AspNetCore.Mvc;

namespace RegistrationFormProject.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}