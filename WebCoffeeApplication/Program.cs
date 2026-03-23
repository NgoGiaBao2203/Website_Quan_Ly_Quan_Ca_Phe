using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.DataContext;
using WebCoffeeApplication.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<TaiKhoan, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = false;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDataContext>()
.AddDefaultTokenProviders();

// Định cấu hình cookie ứng dụng
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

// CCấu hình quy trình xử lý yêu cầu HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Giá trị HSTS mặc định là 30 ngày. Có thể muốn thay đổi giá trị này cho các trường hợp sản xuất, xem thêm tại đây. https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // Thêm đoạn mã này TRƯỚC khi sử dụng UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();

    if (!await roleManager.RoleExistsAsync("ChuQuan"))
    {
        await roleManager.CreateAsync(new IdentityRole<int> { Name = "ChuQuan" });
    }

    if (!await roleManager.RoleExistsAsync("NhanVien"))
    {
        await roleManager.CreateAsync(new IdentityRole<int> { Name = "NhanVien" });
    }

    var adminUser = await userManager.FindByNameAsync("admin");

    if (adminUser == null)
    {
        adminUser = new TaiKhoan
        {
            UserName = "admin",
            Email = "admin@coffee.com",
            HoTen = "Quản trị viên",
            VaiTro = 1,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now,
            SDT = "1234567890"
        };

        var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "ChuQuan");
        }
    }
    else
    {
        if (!await userManager.IsInRoleAsync(adminUser, "ChuQuan"))
        {
            await userManager.AddToRoleAsync(adminUser, "ChuQuan");
        }
    }
}

app.Run();
