using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Заглушка для авторизации
            TempData["Message"] = "Успешный вход в систему!";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string email, string password)
        {
            // Заглушка для регистрации
            TempData["Message"] = "Регистрация успешна! Теперь вы можете войти.";
            return RedirectToAction("Login");
        }
    }
}
