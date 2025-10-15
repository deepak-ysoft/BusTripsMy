using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Identity;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.CustomMiddleware;
using BusTrips.Web.Hubs;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using BusTrips.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog; // Added
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);


// ------------------- Ensure logs folder exists -------------------
var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
}

// ------------------- Configure Serilog -------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logPath, "app-.log"),   // logs/app-YYYYMMDD.log
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        restrictedToMinimumLevel: LogEventLevel.Error,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// -------------------------------------------------------
// Database + Identity + Services
// -------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefConn")));

builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/LoginUser";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
    options.Cookie.IsEssential = true;
});

// -------------------------------------------------------
// Scoped Services
// -------------------------------------------------------
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IOrganizationPermissionService, OrganizationPermissionService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSignalR();

// -------------------------------------------------------
// Global Filters (including your GlobalExceptionFilter)
// -------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RequireWizardCompletionFilter>();
    options.Filters.Add<GlobalExceptionFilter>();
})
.AddViewOptions(options =>
{
    options.HtmlHelperOptions.ClientValidationEnabled = true;
});

// -------------------------------------------------------
// Token Expiration Config
// -------------------------------------------------------
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(1);
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.ValueCountLimit = int.MaxValue;
});

// -------------------------------------------------------
// Build App
// -------------------------------------------------------
var app = builder.Build();

// -------------------------------------------------------
// DB Initialization + Seeding
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await SeedData.SeedAsync(users, roles);
}

// -------------------------------------------------------
// Middleware Pipeline
// -------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapHub<NotificationHub>("/notificationHub");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=FirstAccessPage}/{id?}");

// -------------------------------------------------------
// Run the App
// -------------------------------------------------------
try
{
    Log.Information("🚀 Application starting up...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush();
}
