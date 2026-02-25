using Microsoft.AspNetCore.Mvc;

namespace Agriculture.Controllers
{
    public class FarmWalletController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
