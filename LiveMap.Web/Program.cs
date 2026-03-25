using LiveMap.Core.Contracts;
using LiveMap.Core.Services;
using LiveMap.Data;
using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using LiveMap.Core.Utilities;
using Microsoft.AspNetCore.Identity;

namespace LiveMap.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<LiveMapDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<LiveMapDbContext>();
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<IFolderService, FolderService>();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IAdminService, AdminService>();

            builder.Services.Configure<CloudinarySettings>(
            builder.Configuration.GetSection("CloudinarySettings"));

            var cloudinarySettings = builder.Configuration
                .GetSection("CloudinarySettings")
                .Get<CloudinarySettings>() ?? throw new InvalidOperationException("Cloudinary settings are missing.");

            var account = new Account(
                cloudinarySettings.CloudName,
                cloudinarySettings.ApiKey,
                cloudinarySettings.ApiSecret);

            var cloudinary = new Cloudinary(account);
            cloudinary.Api.Secure = true;

            builder.Services.AddSingleton(cloudinary);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var adminService = services.GetRequiredService<IAdminService>();
                var adminEmails = builder.Configuration
                    .GetSection("AdminSettings:Emails")
                    .Get<string[]>() ?? Array.Empty<string>();

                await adminService.EnsureRolesAndAdminsAsync(adminEmails);

                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
                }

                var user = await userManager.FindByEmailAsync("admin@gmail.com");

                if (user != null && !await userManager.IsInRoleAsync(user, "Admin"))
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
