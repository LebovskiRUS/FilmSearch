using FilmSearch.Data;
using FilmSearch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Services
{
    public interface IAccountService
    {
        Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, User? User)> RegisterAsync(
            string username,
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            username = username.Trim();
            email = email.Trim().ToLowerInvariant();

            if (username.Length < 3 || username.Length > 20)
            {
                return (false, "Имя пользователя должно быть от 3 до 20 символов.", null);
            }

            if (password.Length < 6)
            {
                return (false, "Пароль должен содержать не менее 6 символов.", null);
            }

            var exists = await _context.Users.AnyAsync(
                user => user.Username == username || user.Email == email,
                cancellationToken);

            if (exists)
            {
                return (false, "Пользователь с таким именем или email уже существует.", null);
            }

            var user = new User
            {
                Username = username,
                Email = email,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                IsMovieLensUser = false
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return (true, "Регистрация успешна.", user);
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            email = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(item => item.Email == email && !item.IsMovieLensUser, cancellationToken);

            if (user?.PasswordHash is null)
            {
                return (false, "Неверный email или пароль.", null);
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                return (false, "Неверный email или пароль.", null);
            }

            return (true, "Успешный вход в систему.", user);
        }
    }
}
