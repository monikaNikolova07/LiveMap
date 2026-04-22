
using System.Diagnostics;
using System.Security.Claims;
using LiveMap.Core.Services;
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
            if (User.IsInRole(AdminService.AdminRoleName))
            {
                return RedirectToAction("Index", "Admin");
            }

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
            if (User.IsInRole(AdminService.AdminRoleName))
            {
                return RedirectToAction("Index", "Admin");
            }

            var currentUserId = GetCurrentUserId();
            var currentUserIsAdmin = User.IsInRole(AdminService.AdminRoleName);
            var normalizedFilter = NormalizeFilter(filter);
            var normalizedUsername = (username ?? string.Empty).Trim();

            var pictures = await _context.Pictures
                .Include(p => p.Folder)
                    .ThenInclude(f => f.Profile)
                        .ThenInclude(pr => pr.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Where(p => p.Acssesability == Acssesability.Public)
                .Where(p => p.Folder.Acssesability == Acssesability.Public)
                .Where(p => p.Folder.Profile.Acssesability == Acssesability.Public)
                .Where(p => !_context.UserRoles.Any(ur => ur.UserId == p.Folder.Profile.UserId && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName)))
                .ToListAsync();

            IEnumerable<Picture> filteredPictures = pictures;
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
                    filteredPictures = filteredPictures.OrderBy(p => p.CreatedOn);
                    break;

                case ExploreFilterOptions.ByUser:
                    model.Heading = string.IsNullOrWhiteSpace(normalizedUsername)
                        ? "Search public pictures by user"
                        : $"Public pictures by {normalizedUsername}";
                    model.Description = string.IsNullOrWhiteSpace(normalizedUsername)
                        ? "Type a username and get all their public pictures from newest to oldest."
                        : "These are the public uploads for the selected user, ordered from newest to oldest.";

                    filteredPictures = filteredPictures
                        .Where(p => !string.IsNullOrWhiteSpace(normalizedUsername)
                            && !string.IsNullOrWhiteSpace(p.Folder.Profile.User.UserName)
                            && p.Folder.Profile.User.UserName.Contains(normalizedUsername, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => p.CreatedOn);
                    break;

                case ExploreFilterOptions.PopularUsers:
                    model.Heading = "Pictures from the most popular users";
                    model.Description = "Public pictures from creators with the highest followers count appear first.";
                    filteredPictures = filteredPictures
                        .OrderByDescending(p => _context.UserFollowings.Count(uf => uf.FolowingId == p.Folder.Profile.UserId))
                        .ThenByDescending(p => p.CreatedOn);
                    break;

                case ExploreFilterOptions.FollowingNewest:
                    model.Heading = "Newest pictures from people you follow";
                    model.Description = "The latest public uploads from the profiles you follow.";

                    if (currentUserId == null)
                    {
                        model.RequiresLoginNotice = true;
                        filteredPictures = Enumerable.Empty<Picture>();
                    }
                    else
                    {
                        var followingIds = await _context.UserFollowings
                            .Where(uf => uf.UserId == currentUserId.Value)
                            .Select(uf => uf.FolowingId)
                            .ToListAsync();

                        filteredPictures = filteredPictures
                            .Where(p => followingIds.Contains(p.Folder.Profile.UserId))
                            .OrderByDescending(p => p.CreatedOn);
                    }
                    break;

                case ExploreFilterOptions.FriendsNewest:
                    model.Heading = "Newest pictures from your friends";
                    model.Description = "Latest public uploads from users with mutual follows with you.";

                    if (currentUserId == null)
                    {
                        model.RequiresLoginNotice = true;
                        filteredPictures = Enumerable.Empty<Picture>();
                    }
                    else
                    {
                        var followingIds = await _context.UserFollowings
                            .Where(uf => uf.UserId == currentUserId.Value)
                            .Select(uf => uf.FolowingId)
                            .ToListAsync();

                        var followerIds = await _context.UserFollowings
                            .Where(uf => uf.FolowingId == currentUserId.Value)
                            .Select(uf => uf.UserId)
                            .ToListAsync();

                        var friendIds = followingIds.Intersect(followerIds).ToHashSet();
                        filteredPictures = filteredPictures
                            .Where(p => friendIds.Contains(p.Folder.Profile.UserId))
                            .OrderByDescending(p => p.CreatedOn);
                    }
                    break;

                default:
                    model.Heading = "Newest public pictures";
                    model.Description = "Browse the latest uploaded public images and open the creator profiles.";
                    filteredPictures = filteredPictures.OrderByDescending(p => p.CreatedOn);
                    break;
            }

            model.Pictures = filteredPictures
                .Take(48)
                .Select(p => MapExplorePicture(p, currentUserId, currentUserIsAdmin))
                .ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Country(string country)
        {
            if (User.IsInRole(AdminService.AdminRoleName))
            {
                return RedirectToAction("Index", "Admin");
            }

            var currentUserId = GetCurrentUserId();
            var currentUserIsAdmin = User.IsInRole(AdminService.AdminRoleName);

            if (!CountryUiHelper.TryParseCountry(country, out var selectedCountry))
            {
                return RedirectToAction(nameof(Index));
            }

            var normalizedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                CountryUiHelper.NormalizeKey(selectedCountry.ToString()),
                CountryUiHelper.NormalizeKey(CountryUiHelper.GetDisplayName(selectedCountry))
            };

            var pictures = await _context.Pictures
                .Include(p => p.Folder)
                    .ThenInclude(f => f.Profile)
                        .ThenInclude(pr => pr.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Where(p => p.Acssesability == Acssesability.Public)
                .Where(p => p.Folder.Acssesability == Acssesability.Public)
                .Where(p => p.Folder.Profile.Acssesability == Acssesability.Public)
                .Where(p => !_context.UserRoles.Any(ur => ur.UserId == p.Folder.Profile.UserId && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName)))
                .ToListAsync();

            var gallery = new CountryGalleryViewModel
            {
                CountryValue = selectedCountry.ToString(),
                CountryName = CountryUiHelper.GetDisplayName(selectedCountry),
                CountryFlagEmoji = CountryUiHelper.GetFlagEmoji(selectedCountry),
                Images = pictures
                    .Where(p => normalizedKeys.Contains(CountryUiHelper.NormalizeKey(p.Folder.Name ?? string.Empty)))
                    .Select(p => new CountryImageCardViewModel
                    {
                        PictureId = p.Id,
                        ImageUrl = p.URL,
                        FolderId = p.FolderId,
                        FolderName = p.Folder.Name,
                        ProfileId = p.Folder.ProfileId,
                        Username = p.Folder.Profile.User.UserName ?? "Unknown creator",
                        ProfilePicture = p.Folder.Profile.ProfilePicture ?? string.Empty,
                        Description = p.Description,
                        LikesCount = p.Likes.Count,
                        CommentsCount = p.Comments.Count,
                        IsLikedByCurrentUser = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value),
                        CanInteract = currentUserId.HasValue && !currentUserIsAdmin && p.Folder.Profile.UserId != currentUserId.Value,
                        CreatedOn = p.CreatedOn,
                        Comments = p.Comments
                            .OrderByDescending(c => c.CreatedOn)
                            .Take(3)
                            .Select(c => new ExploreCommentViewModel
                            {
                                Username = c.User.UserName ?? "Unknown user",
                                Content = c.Content,
                                CreatedOn = c.CreatedOn
                            })
                            .ToList()
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

        private ExplorePictureViewModel MapExplorePicture(Picture picture, Guid? currentUserId, bool currentUserIsAdmin)
        {
            return new ExplorePictureViewModel
            {
                PictureId = picture.Id,
                FolderId = picture.FolderId,
                ImageUrl = picture.URL,
                ProfileId = picture.Folder.Profile.Id,
                Username = picture.Folder.Profile.User.UserName ?? "Unknown creator",
                ProfilePicture = picture.Folder.Profile.ProfilePicture ?? string.Empty,
                FolderName = picture.Folder.Name,
                Description = picture.Description,
                CreatedOn = picture.CreatedOn,
                FollowersCount = _context.UserFollowings.Count(uf => uf.FolowingId == picture.Folder.Profile.UserId),
                LikesCount = picture.Likes.Count,
                CommentsCount = picture.Comments.Count,
                IsLikedByCurrentUser = currentUserId.HasValue && picture.Likes.Any(l => l.UserId == currentUserId.Value),
                CanInteract = currentUserId.HasValue && !currentUserIsAdmin && picture.Folder.Profile.UserId != currentUserId.Value,
                Comments = picture.Comments
                    .OrderByDescending(c => c.CreatedOn)
                    .Take(3)
                    .Select(c => new ExploreCommentViewModel
                    {
                        Username = c.User.UserName ?? "Unknown user",
                        Content = c.Content,
                        CreatedOn = c.CreatedOn
                    })
                    .ToList()
            };
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
    }
}



