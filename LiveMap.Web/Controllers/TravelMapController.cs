using System.Security.Claims;
using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Web.Helpers;
using LiveMap.Core.Services;
using LiveMap.Web.Models.TravelMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class TravelMapController : Controller
    {
        private static readonly string[] Palette =
        {
            "#facc15", "#fb7185", "#f97316", "#22c55e", "#14b8a6",
            "#38bdf8", "#6366f1", "#a855f7", "#8b5e3c", "#64748b"
        };

        private const string DefaultColor = "#facc15";
        private readonly LiveMapDbContext context;

        public TravelMapController(LiveMapDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Challenge();
            }

            var isAdmin = await context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName));

            if (isAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            var folders = await context.Folders
                .AsNoTracking()
                .Where(f => f.Profile.UserId == userId)
                .Select(f => new
                {
                    f.Name,
                    PictureCount = f.Pictures.Count
                })
                .Where(f => f.PictureCount > 0)
                .ToListAsync();

            var savedColors = await context.UserCountryMapColors
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToDictionaryAsync(x => x.Country, x => x.ColorHex);

            var visitedCountries = folders
                .Select(folder => new
                {
                    FolderName = folder.Name,
                    CountryMatched = CountryUiHelper.TryParseCountry(folder.Name, out var country) ? country : (Country?)null,
                    folder.PictureCount
                })
                .Where(x => x.CountryMatched.HasValue)
                .GroupBy(x => x.CountryMatched!.Value)
                .Select(group => new TravelMapCountryViewModel
                {
                    CountryValue = group.Key.ToString(),
                    CountryName = CountryUiHelper.GetDisplayName(group.Key),
                    ColorHex = savedColors.TryGetValue(group.Key.ToString(), out var color) && Palette.Contains(color, StringComparer.OrdinalIgnoreCase)
                        ? color
                        : DefaultColor,
                    FolderCount = group.Count(),
                    PictureCount = group.Sum(x => x.PictureCount)
                })
                .OrderBy(x => x.CountryName)
                .ToList();

            var viewModel = new TravelMapIndexViewModel
            {
                DefaultColor = DefaultColor,
                Palette = Palette.ToList(),
                VisitedCountries = visitedCountries,
                CountryAliases = CountryUiHelper.GetClientAliasMap()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveColor([FromBody] TravelMapColorRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var isAdmin = await context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName));

            if (isAdmin)
            {
                return Forbid();
            }

            if (request == null || !CountryUiHelper.TryParseCountry(request.Country, out var country))
            {
                return BadRequest(new { message = "Invalid country." });
            }

            var normalizedColor = (request.ColorHex ?? string.Empty).Trim();
            if (!Palette.Contains(normalizedColor, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Invalid color." });
            }

            var ownedFolderNames = await context.Folders
                .AsNoTracking()
                .Where(f => f.Profile.UserId == userId && f.Pictures.Any())
                .Select(f => f.Name)
                .ToListAsync();

            var hasPicturesForCountry = ownedFolderNames.Any(folderName =>
                CountryUiHelper.TryParseCountry(folderName, out var folderCountry) && folderCountry == country);

            if (!hasPicturesForCountry)
            {
                return BadRequest(new { message = "You can color only countries that already have uploaded images." });
            }

            var countryKey = country.ToString();
            var existingRecord = await context.UserCountryMapColors
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Country == countryKey);

            if (existingRecord == null)
            {
                existingRecord = new UserCountryMapColor
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Country = countryKey,
                    ColorHex = normalizedColor
                };

                await context.UserCountryMapColors.AddAsync(existingRecord);
            }
            else
            {
                existingRecord.ColorHex = normalizedColor;
            }

            await context.SaveChangesAsync();

            return Json(new { success = true, country = countryKey, colorHex = normalizedColor });
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) ? userId : Guid.Empty;
        }

        public class TravelMapColorRequest
        {
            public string Country { get; set; } = string.Empty;
            public string ColorHex { get; set; } = string.Empty;
        }
    }
}
