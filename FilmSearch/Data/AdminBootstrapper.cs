using FilmSearch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Data
{
    public class AdminBootstrapper
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AdminBootstrapper(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task EnsureAdminAsync(CancellationToken cancellationToken = default)
        {
            var email = _configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
            var username = _configuration["BootstrapAdmin:Username"]?.Trim();
            var password = _configuration["BootstrapAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(user => user.Email == email || user.Username == username, cancellationToken);

            if (admin is null)
            {
                admin = new User
                {
                    CreatedAt = DateTime.UtcNow,
                    IsMovieLensUser = false
                };

                _context.Users.Add(admin);
            }

            admin.Username = username;
            admin.Email = email;
            admin.Role = "Admin";
            admin.IsMovieLensUser = false;
            admin.PasswordHash = _passwordHasher.HashPassword(admin, password);

            if (admin.CreatedAt == default)
            {
                admin.CreatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
