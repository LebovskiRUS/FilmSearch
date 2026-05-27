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

        public DbSet<User> Users => Set<User>();
        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Rating> Ratings => Set<Rating>();
        public DbSet<RecommendationEvent> RecommendationEvents => Set<RecommendationEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.IsMovieLensUser).HasColumnName("is_movielens_user");
                entity.Property(e => e.MovieLensUserId).HasColumnName("movielens_user_id");
                entity.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(1);
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.Occupation).HasColumnName("occupation");
                entity.Property(e => e.ZipCode).HasColumnName("zip_code").HasMaxLength(20);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.MovieLensUserId).IsUnique();
            });

            modelBuilder.Entity<Movie>(entity =>
            {
                entity.ToTable("movies");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MovieLensId).HasColumnName("movielens_id");
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
                entity.Property(e => e.Year).HasColumnName("year");
                entity.Property(e => e.Genres).HasColumnName("genres").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
                entity.Property(e => e.AverageRating).HasColumnName("average_rating").HasPrecision(4, 2);
                entity.Property(e => e.RatingsCount).HasColumnName("ratings_count");

                entity.HasIndex(e => e.MovieLensId).IsUnique();
                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => e.Year);
            });

            modelBuilder.Entity<Rating>(entity =>
            {
                entity.ToTable("ratings");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.MovieId).HasColumnName("movie_id");
                entity.Property(e => e.Value).HasColumnName("value").IsRequired();
                entity.Property(e => e.RatedAt).HasColumnName("rated_at");
                entity.Property(e => e.SourceTimestamp).HasColumnName("source_timestamp");

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Ratings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                    .WithMany(e => e.Ratings)
                    .HasForeignKey(e => e.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.MovieId }).IsUnique();
                entity.HasIndex(e => e.MovieId);
            });

            modelBuilder.Entity<RecommendationEvent>(entity =>
            {
                entity.ToTable("recommendation_events");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.MovieId).HasColumnName("movie_id");
                entity.Property(e => e.Score).HasColumnName("score").HasPrecision(5, 3);
                entity.Property(e => e.ModelVersion).HasColumnName("model_version").HasMaxLength(100);
                entity.Property(e => e.ShownAt).HasColumnName("shown_at");
                entity.Property(e => e.ClickedAt).HasColumnName("clicked_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                    .WithMany()
                    .HasForeignKey(e => e.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.ShownAt });
            });
        }
    }
}
