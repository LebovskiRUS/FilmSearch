using System.Security.Claims;
using FilmSearch.Models;
using FilmSearch.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace FilmSearch.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, CancellationToken cancellationToken)
        {
            var result = await _accountService.LoginAsync(email, password, cancellationToken);
            if (!result.Success || result.User is null)
            {
                TempData["Error"] = result.Message;
                return View();
            }

            await SignInAsync(result.User);
            TempData["Message"] = result.Message;
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            string username,
            string email,
            string password,
            string confirmPassword,
            CancellationToken cancellationToken)
        {
            if (password != confirmPassword)
            {
                TempData["Error"] = "Пароли не совпадают.";
                return View();
            }

            var result = await _accountService.RegisterAsync(username, email, password, cancellationToken);
            if (!result.Success || result.User is null)
            {
                TempData["Error"] = result.Message;
                return View();
            }

            await SignInAsync(result.User);
            TempData["Message"] = "Регистрация успешна.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Message"] = "Вы вышли из системы.";
            return RedirectToAction("Index", "Home");
        }

        private Task SignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role)
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
