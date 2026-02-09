using System.Threading.RateLimiting;
using Cartrade.Data;
using Cartrade.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var provider = (builder.Configuration["DatabaseProvider"] ?? "sqlite").ToLowerInvariant();
var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var mySqlConnection = builder.Configuration.GetConnectionString("MySqlConnection") ?? string.Empty;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (provider == "mysql")
    {
        if (string.IsNullOrWhiteSpace(mySqlConnection))
        {
            throw new InvalidOperationException("DatabaseProvider is 'mysql' but 'MySqlConnection' is missing.");
        }

        options.UseMySQL(mySqlConnection, _ => { });
        return;
    }

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<CsvVehicleImporter>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(SeedData.AdminRole));
    options.AddPolicy("InternalOnly",
        policy => policy.RequireRole(SeedData.AdminRole, SeedData.InspectionRole, SeedData.FinanceRole, SeedData.SalesRole));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

await SeedData.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
    {
        if (HttpMethods.IsGet(context.Request.Method) &&
            context.User.Identity?.IsAuthenticated == true &&
            context.User.IsInRole(SeedData.AdminRole))
        {
            context.Response.Redirect("/AdminUsers/Create");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
