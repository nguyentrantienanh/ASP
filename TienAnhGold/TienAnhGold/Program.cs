using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TienAnhGold.Data;
using TienAnhGold.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<TienAnhGoldContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TienAnhGoldContext")));

// Thêm dịch vụ session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";
        options.AccessDeniedPath = "/Home/AccessDenied"; // Đảm bảo action AccessDenied tồn tại
        options.SlidingExpiration = true;
    });

// Cấu hình authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
    options.AddPolicy("RequireAdminOrEmployee", policy => policy.RequireRole("Admin", "Employee"));
    options.AddPolicy("AdminOrEmployee", policy => policy.RequireRole("Admin", "Employee")); // Chính sách này cho phép cả Admin và Employee
});

builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();