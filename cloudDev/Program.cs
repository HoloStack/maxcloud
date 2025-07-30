using cloudDev.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Configure culture for consistent decimal parsing globally
var cultureInfo = new CultureInfo("en-US");
// Set both current and default cultures
CultureInfo.CurrentCulture = cultureInfo;
CultureInfo.CurrentUICulture = cultureInfo;
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Ensure all threads use this culture
Thread.CurrentThread.CurrentCulture = cultureInfo;
Thread.CurrentThread.CurrentUICulture = cultureInfo;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure localization for model binding
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en-US" };
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    })
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        googleOptions.CallbackPath = "/signin-google";
        
        // Request additional scopes
        googleOptions.Scope.Add("email");
        googleOptions.Scope.Add("profile");
        
        // Save tokens for later use if needed
        googleOptions.SaveTokens = true;
    });

// Add Azure services
builder.Services.AddSingleton<TableStorageService>();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Use request localization
app.UseRequestLocalization();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Database seeding is disabled
// To enable seeding, uncomment the code below:
/*
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    try
    {
        Console.WriteLine("🌱 Initializing database...");
        await seeder.SeedDatabaseAsync();
        Console.WriteLine("✅ Database initialization completed!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database seeding failed: {ex.Message}");
    }
}
*/

app.Run();
