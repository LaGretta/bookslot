using BookSlot.Data;
using BookSlot.Features.AiAssistant;
using BookSlot.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

var rawConnection =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No database connection string.");

// Railway provides postgres:// URIs; convert to Npgsql key=value format
var connectionString = rawConnection.StartsWith("postgres")
    ? ToNpgsqlConnectionString(rawConnection)
    : rawConnection;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount   = false;
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase       = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan    = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Cookie.MaxAge     = TimeSpan.FromDays(14);
    options.Cookie.HttpOnly   = true;
    options.LoginPath         = "/Identity/Account/Login";
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("BookSlot");

builder.Services.AddRazorPages();
builder.Services.AddScoped<BookingService>();
builder.Services.AddHttpClient<ResendEmailService>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();
builder.Services.AddScoped<IEmailSender, IdentityEmailSender>(); // "Forgot password" emails
builder.Services.AddScoped<StripeService>();
if (builder.Configuration.GetValue("BookingReminders:Enabled", true))
{
    builder.Services.AddHostedService<BookingReminderWorker>();
}
builder.Services.AddAiAssistantFeature(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

static string ToNpgsqlConnectionString(string url)
{
    try
    {
        var uri      = new Uri(url);
        var userParts = uri.UserInfo.Split(':');
        var user     = userParts[0];
        var password = userParts.Length > 1 ? Uri.UnescapeDataString(userParts[1]) : "";
        var host     = uri.Host;
        var dbPort   = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        return $"Host={host};Port={dbPort};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch
    {
        return url;
    }
}
