using DNTCaptcha.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RegistrationFormProject.Data;
using RegistrationFormProject.Filters;
using RegistrationFormProject.Repositories;
using RegistrationFormProject.Repositories.Interfaces;
using RegistrationFormProject.Services;
using RegistrationFormProject.Models;
using RegistrationFormProject.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<KycVerificationFilter>();
    options.Filters.Add<XssProtectionFilter>();
    options.Filters.Add<ExceptionLogFilter>();
});
builder.Services.AddScoped<KycVerificationFilter>();
builder.Services.AddScoped<XssProtectionFilter>();
builder.Services.AddScoped<ExceptionLogFilter>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<DapperContext>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddDNTCaptcha(options =>
{
    options.UseCookieStorageProvider()
           .ShowThousandsSeparators(false)
           .WithEncryptionKey("MyVeryStrongKey123");
});


builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<
    IEmailService,
    EmailService>();
builder.Services.Configure<TwilioSettings>(
    builder.Configuration.GetSection("TwilioSettings"));
var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    // Set all existing users to Active (since column default was false in DB)
//    var inactiveUsers = context.UserMasters.Where(u => !u.IsActive).ToList();
//    foreach (var u in inactiveUsers)
//    {
//        u.IsActive = true;
//    }
//    // Set first admin as SuperAdmin
//    var firstAdmin = context.UserMasters.FirstOrDefault(u => u.RoleId == 1);
//    if (firstAdmin != null)
//    {
//        firstAdmin.IsSuperAdmin = true;
//    }
//    context.SaveChanges();
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<RequestPerformanceMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}");

app.Run();
