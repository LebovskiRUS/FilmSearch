using FilmSearch.Data;
using FilmSearch.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<AdminBootstrapper>();
builder.Services.AddScoped<IMovieLensImportService, MovieLensImportService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IAdminMovieService, AdminMovieService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IRecommendationService, BaselineRecommendationService>();
builder.Services.Configure<MlRecommendationOptions>(builder.Configuration.GetSection("MlRecommendation"));
builder.Services.AddHttpClient<IMlRecommendationClient, MlRecommendationClient>((serviceProvider, httpClient) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MlRecommendationOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();

    var adminBootstrapper = scope.ServiceProvider.GetRequiredService<AdminBootstrapper>();
    await adminBootstrapper.EnsureAdminAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
