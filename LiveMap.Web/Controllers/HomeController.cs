using System.Diagnostics;
using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Web.Helpers;
using LiveMap.Web.Models;
using LiveMap.Web.Models.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

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
        public async Task<IActionResult> Explore(string filter = ExploreFilterOptions.Newest, string? username = null)
        {
            var currentUserId = GetCurrentUserId();
            var normalizedFilter = NormalizeFilter(filter);
            var normalizedUsername = (username ?? string.Empty).Trim();

            var publicPictures = _context.Pictures
                .Where(p => p.Acssesability == Acssesability.Public)
                .Where(p => p.Folder.Acssesability == Acssesability.Public)
                .Where(p => p.Folder.Profile.Acssesability == Acssesability.Public);

            IQueryable<ExplorePictureViewModel>? pictureQuery = null;
            List<ExplorePictureViewModel>? precomputedPictures = null;
            var model = new ExploreFeedViewModel
            {
                SelectedFilter = normalizedFilter,
                UsernameQuery = normalizedUsername
            };

            switch (normalizedFilter)
            {
                case ExploreFilterOptions.Oldest:
                    model.Heading = "Oldest public pictures";
                    model.Description = "See the oldest public uploads first across all public profiles.";
                    pictureQuery = publicPictures
                        .OrderBy(p => p.CreatedOn)
                        .Select(ToExplorePicture());
                    break;

                case ExploreFilterOptions.ByUser:
                    model.Heading = string.IsNullOrWhiteSpace(normalizedUsername)
                        ? "Search public pictures by user"
                        : $"Public pictures by {normalizedUsername}";
                    model.Description = string.IsNullOrWhiteSpace(normalizedUsername)
                        ? "Type a username and get all their public pictures from newest to oldest."
                        : "These are the public uploads for the selected user, ordered from newest to oldest.";

                    pictureQuery = publicPictures
                        .Where(p => !string.IsNullOrWhiteSpace(normalizedUsername)
                            && p.Folder.Profile.User.UserName != null
                            && EF.Functions.Like(p.Folder.Profile.User.UserName, $"%{normalizedUsername}%"))
                        .OrderByDescending(p => p.CreatedOn)
                        .Select(ToExplorePicture());
                    break;

                case ExploreFilterOptions.PopularUsers:
                    model.Heading = "Pictures from the most popular users";
                    model.Description = "Public pictures from creators with the highest followers count appear first.";
                    pictureQuery = publicPictures
                        .Select(p => new
                        {
                            Picture = p,
                            FollowersCount = _context.UserFollowings.Count(uf => uf.FolowingId == p.Folder.Profile.UserId)
                        })
                        .OrderByDescending(x => x.FollowersCount)
                        .ThenByDescending(x => x.Picture.CreatedOn)
                        .Select(x => new ExplorePictureViewModel
                        {
                            PictureId = x.Picture.Id,
                            ImageUrl = x.Picture.URL,
                            ProfileId = x.Picture.Folder.Profile.Id,
                            Username = x.Picture.Folder.Profile.User.UserName ?? "Unknown creator",
                            ProfilePicture = x.Picture.Folder.Profile.ProfilePicture ?? string.Empty,
                            FolderName = x.Picture.Folder.Name,
                            CreatedOn = x.Picture.CreatedOn,
                            FollowersCount = x.FollowersCount
                        });
                    break;

                case ExploreFilterOptions.FollowingNewest:
                    model.Heading = "Newest pictures from people you follow";
                    model.Description = "The latest public uploads from the profiles you follow.";

                    if (currentUserId == null)
                    {
                        model.RequiresLoginNotice = true;
                        precomputedPictures = new List<ExplorePictureViewModel>();
                    }
                    else
                    {
                        pictureQuery = publicPictures
                            .Where(p => _context.UserFollowings.Any(uf => uf.UserId == currentUserId.Value && uf.FolowingId == p.Folder.Profile.UserId))
                            .OrderByDescending(p => p.CreatedOn)
                            .Select(ToExplorePicture());
                    }
                    break;

                case ExploreFilterOptions.FriendsNewest:
                    model.Heading = "Newest pictures from your friends";
                    model.Description = "Latest public uploads from users with mutual follows with you.";

                    if (currentUserId == null)
                    {
                        model.RequiresLoginNotice = true;
                        precomputedPictures = new List<ExplorePictureViewModel>();
                    }
                    else
                    {
                        pictureQuery = publicPictures
                            .Where(p => _context.UserFollowings.Any(uf => uf.UserId == currentUserId.Value && uf.FolowingId == p.Folder.Profile.UserId)
                                     && _context.UserFollowings.Any(back => back.UserId == p.Folder.Profile.UserId && back.FolowingId == currentUserId.Value))
                            .OrderByDescending(p => p.CreatedOn)
                            .Select(ToExplorePicture());
                    }
                    break;

                default:
                    model.Heading = "Newest public pictures";
                    model.Description = "Browse the latest uploaded public images and open the creator profiles.";
                    pictureQuery = publicPictures
                        .OrderByDescending(p => p.CreatedOn)
                        .Select(ToExplorePicture());
                    break;
            }

            model.Pictures = precomputedPictures ?? await pictureQuery!.Take(48).ToListAsync();
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

        private Guid? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }

        private static string NormalizeFilter(string? filter)
        {
            return (filter ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                ExploreFilterOptions.Oldest => ExploreFilterOptions.Oldest,
                ExploreFilterOptions.ByUser => ExploreFilterOptions.ByUser,
                ExploreFilterOptions.PopularUsers => ExploreFilterOptions.PopularUsers,
                ExploreFilterOptions.FollowingNewest => ExploreFilterOptions.FollowingNewest,
                ExploreFilterOptions.FriendsNewest => ExploreFilterOptions.FriendsNewest,
                _ => ExploreFilterOptions.Newest
            };
        }

        private static Expression<Func<Picture, ExplorePictureViewModel>> ToExplorePicture()
        {
            return p => new ExplorePictureViewModel
            {
                PictureId = p.Id,
                ImageUrl = p.URL,
                ProfileId = p.Folder.Profile.Id,
                Username = p.Folder.Profile.User.UserName ?? "Unknown creator",
                ProfilePicture = p.Folder.Profile.ProfilePicture ?? string.Empty,
                FolderName = p.Folder.Name,
                CreatedOn = p.CreatedOn,
                FollowersCount = 0
            };
        }
    }
}
