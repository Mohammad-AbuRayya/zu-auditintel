using Microsoft.AspNetCore.Mvc;

namespace ZU_AuditIntel.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
