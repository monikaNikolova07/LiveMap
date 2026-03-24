using System.Diagnostics;
using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Web.Helpers;
using LiveMap.Web.Models;
using LiveMap.Web.Models.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LiveMapDbContext _context;

        public HomeController(ILogger<HomeController> logger, LiveMapDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new HomeIndexViewModel
            {
                Countries = Enum.GetValues<Country>()
                    .Select(country => new CountryOptionViewModel
                    {
                        Value = country.ToString(),
                        Name = CountryUiHelper.GetDisplayName(country),
                        FlagEmoji = CountryUiHelper.GetFlagEmoji(country)
                    })
                    .OrderBy(country => country.Name)
                    .ToList(),
                CountryAliases = CountryUiHelper.GetClientAliasMap()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Country(string country)
        {
            if (!CountryUiHelper.TryParseCountry(country, out var selectedCountry))
            {
                return RedirectToAction(nameof(Index));
            }

            var normalizedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                CountryUiHelper.NormalizeKey(selectedCountry.ToString()),
                CountryUiHelper.NormalizeKey(CountryUiHelper.GetDisplayName(selectedCountry))
            };

            var images = await _context.Pictures
                .Where(p => p.Acssesability == Acssesability.Public)
                .Select(p => new
                {
                    p.Id,
                    p.URL,
                    p.FolderId,
                    FolderName = p.Folder.Name,
                    FolderAccessibility = p.Folder.Acssesability,
                    ProfileId = p.Folder.ProfileId,
                    ProfileAccessibility = p.Folder.Profile.Acssesability,
                    Username = p.Folder.Profile.User.UserName,
                    ProfilePicture = p.Folder.Profile.ProfilePicture
                })
                .ToListAsync();

            var gallery = new CountryGalleryViewModel
            {
                CountryValue = selectedCountry.ToString(),
                CountryName = CountryUiHelper.GetDisplayName(selectedCountry),
                CountryFlagEmoji = CountryUiHelper.GetFlagEmoji(selectedCountry),
                Images = images
                    .Where(image => image.FolderAccessibility == Acssesability.Public)
                    .Where(image => image.ProfileAccessibility == Acssesability.Public)
                    .Where(image => normalizedKeys.Contains(CountryUiHelper.NormalizeKey(image.FolderName ?? string.Empty)))
                    .Select(image => new CountryImageCardViewModel
                    {
                        PictureId = image.Id,
                        ImageUrl = image.URL,
                        FolderId = image.FolderId,
                        FolderName = image.FolderName ?? string.Empty,
                        ProfileId = image.ProfileId,
                        Username = image.Username ?? "Unknown creator",
                        ProfilePicture = image.ProfilePicture ?? string.Empty
                    })
                    .ToList()
            };

            return View(gallery);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
