using CloudinaryDotNet;
using LiveMap.Core.Contracts;
using LiveMap.Core.Services;
using LiveMap.Core.Utilities;
using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Data.SeedData;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Web
{
    //sdrfyvgmkpl,[.;'ngvgbb gves
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
            builder.Services.AddScoped<JsonSeeder>();

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
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<LiveMapDbContext>();
                var jsonSeeder = services.GetRequiredService<JsonSeeder>();

                await dbContext.Database.MigrateAsync();
                await jsonSeeder.SeedAsync();
            }

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
            app.Run();
        }
    }
}
