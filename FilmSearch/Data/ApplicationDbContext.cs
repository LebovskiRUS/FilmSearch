using FilmSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmSearch.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка таблицы Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Настройка таблицы Movies
            modelBuilder.Entity<Movie>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Genre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);

                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => e.Genre);
                entity.HasIndex(e => e.Year);
            });

            // Настройка таблицы Ratings
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.RatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Связь с User
                entity.HasOne<Rating>()
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Связь с Movie
                entity.HasOne<Rating>()
                    .WithMany()
                    .HasForeignKey(r => r.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Уникальный индекс для пары UserId-MovieId
                entity.HasIndex(e => new { e.UserId, e.MovieId }).IsUnique();

                // Проверка диапазона оценки
                //entity.HasCheckConstraint("CK_Rating_Value", "\"Value\" >= 1 AND \"Value\" <= 5");
            });
        }
    }
}
