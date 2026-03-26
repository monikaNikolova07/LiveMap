using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Core.DTOs.Profiles;
using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService profileService;
        private readonly IImageService imageService;
        private readonly IFolderService folderService;
        private readonly LiveMapDbContext context;

        public ProfileController(IProfileService profileService, IImageService imageService, IFolderService folderService, LiveMapDbContext context)
        {
            this.profileService = profileService;
            this.imageService = imageService;
            this.folderService = folderService;
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var profileId = await context.Profiles
                .Where(p => p.UserId == currentUserId.Value)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (profileId == Guid.Empty)
            {
                return NotFound();
            }

            var model = await BuildProfileViewModelAsync(profileId, currentUserId.Value, false);
            if (model == null)
            {
                return NotFound();
            }

            PopulateFolderCreateLists();
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var model = await BuildProfileViewModelAsync(id, currentUserId, true);
            if (model == null)
            {
                return NotFound();
            }

            if (model.IsOwnProfile)
            {
                PopulateFolderCreateLists();
            }

            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder(FolderCreateDto model)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                var profileId = await context.Profiles.Where(p => p.UserId == currentUserId.Value).Select(p => p.Id).FirstOrDefaultAsync();
                var viewModel = await BuildProfileViewModelAsync(profileId, currentUserId.Value, false);
                if (viewModel == null)
                {
                    return NotFound();
                }

                viewModel.NewFolder = model;
                PopulateFolderCreateLists();
                return View("Index", viewModel);
            }

            await folderService.CreateAsync(model, currentUserId.Value);
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Search(string? term)
        {
            var currentUserId = GetCurrentUserId();

            var query = context.Profiles
                .Where(p => p.Acssesability == Acssesability.Public);

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(p => p.User.UserName != null && p.User.UserName.Contains(term));
            }

            var results = await query
                .OrderBy(p => p.User.UserName)
                .Take(30)
                .Select(p => new ProfileSearchResultViewModel
                {
                    Id = p.Id,
                    Username = p.User.UserName ?? "Unknown user",
                    ProfilePicture = p.ProfilePicture,
                    Bio = p.Bio,
                    FollowersCount = context.UserFollowings.Count(uf => uf.FolowingId == p.UserId),
                    IsOwnProfile = currentUserId != null && p.UserId == currentUserId.Value,
                    IsFollowedByCurrentUser = currentUserId != null && context.UserFollowings.Any(uf => uf.UserId == currentUserId.Value && uf.FolowingId == p.UserId)
                })
                .ToListAsync();

            var model = new ProfileSearchPageViewModel
            {
                Term = term ?? string.Empty,
                Results = results
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var targetProfile = await context.Profiles.FirstOrDefaultAsync(p => p.Id == id);
            if (targetProfile == null)
            {
                return NotFound();
            }

            if (targetProfile.UserId == currentUserId.Value)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            var exists = await context.UserFollowings
                .AnyAsync(uf => uf.UserId == currentUserId.Value && uf.FolowingId == targetProfile.UserId);

            if (!exists)
            {
                await context.UserFollowings.AddAsync(new UserFollowing
                {
                    UserId = currentUserId.Value,
                    FolowingId = targetProfile.UserId,
                    CreatedOn = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            var returnUrl = Request.Headers.Referer.ToString();
            return string.IsNullOrWhiteSpace(returnUrl) ? RedirectToAction(nameof(Details), new { id }) : Redirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var targetProfile = await context.Profiles.FirstOrDefaultAsync(p => p.Id == id);
            if (targetProfile == null)
            {
                return NotFound();
            }

            var relation = await context.UserFollowings
                .FirstOrDefaultAsync(uf => uf.UserId == currentUserId.Value && uf.FolowingId == targetProfile.UserId);

            if (relation != null)
            {
                context.UserFollowings.Remove(relation);
                await context.SaveChangesAsync();
            }

            var returnUrl = Request.Headers.Referer.ToString();
            return string.IsNullOrWhiteSpace(returnUrl) ? RedirectToAction(nameof(Details), new { id }) : Redirect(returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var model = new NotificationsViewModel
            {
                Followers = await context.UserFollowings
                    .Where(uf => uf.FolowingId == currentUserId.Value)
                    .OrderByDescending(uf => uf.CreatedOn)
                    .Take(50)
                    .Select(uf => new NotificationItemViewModel
                    {
                        ProfileId = uf.User.Profile != null ? uf.User.Profile.Id : Guid.Empty,
                        Username = uf.User.UserName ?? "Unknown user",
                        ProfilePicture = uf.User.Profile != null ? uf.User.Profile.ProfilePicture : null,
                        CreatedOn = uf.CreatedOn
                    })
                    .Where(item => item.ProfileId != Guid.Empty)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var profileDto = await profileService.GetProfileForEditAsync(userId);

            if (profileDto == null)
            {
                return NotFound();
            }

            return View(profileDto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProfileEditDto dto, IFormFile? imageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var imageResult = await imageService.UploadImageAsync(
                    imageFile,
                    Guid.NewGuid().ToString(),
                    "profiles");

                dto.ProfilePicture = imageResult.Url;
            }

            await profileService.EditProfileAsync(dto, userId);

            return RedirectToAction(nameof(Index));
        }

        private Guid? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }

        private void PopulateFolderCreateLists()
        {
            ViewBag.Countries = Enum.GetValues(typeof(Country))
                .Cast<Country>()
                .Select(c => new SelectListItem { Text = c.ToString(), Value = c.ToString() })
                .ToList();

            ViewBag.Accessibilities = Enum.GetValues(typeof(Acssesability))
                .Cast<Acssesability>()
                .Select(a => new SelectListItem
                {
                    Text = a == Acssesability.FriendsOnly ? "Friends Only" : a.ToString(),
                    Value = a.ToString()
                })
                .ToList();
        }

        private async Task<ProfileViewModel?> BuildProfileViewModelAsync(Guid profileId, Guid? currentUserId, bool publicFoldersOnly)
        {
            var profileData = await context.Profiles
                .Where(p => p.Id == profileId)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    p.ProfilePicture,
                    p.Bio,
                    Username = p.User.UserName,
                    p.Acssesability,
                    Folders = p.Folders
                        .Where(f => !f.ParentFolders.Any())
                        .Select(f => new
                        {
                            f.Id,
                            f.Name,
                            f.Acssesability
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (profileData == null)
            {
                return null;
            }

            var isOwnProfile = currentUserId != null && profileData.UserId == currentUserId.Value;
            if (publicFoldersOnly && !isOwnProfile && profileData.Acssesability != Acssesability.Public)
            {
                return null;
            }

            var areFriends = false;
            if (currentUserId != null && !isOwnProfile)
            {
                areFriends = await context.UserFollowings.AnyAsync(uf => uf.UserId == currentUserId.Value && uf.FolowingId == profileData.UserId)
                             && await context.UserFollowings.AnyAsync(uf => uf.UserId == profileData.UserId && uf.FolowingId == currentUserId.Value);
            }

            var visibleFolders = profileData.Folders
                .Where(f => isOwnProfile
                    || f.Acssesability == Acssesability.Public
                    || (f.Acssesability == Acssesability.FriendsOnly && areFriends))
                .Select(f => new ProfileFolderViewModel
                {
                    Id = f.Id,
                    Name = f.Name,
                    AccessibilityLabel = f.Acssesability == Acssesability.FriendsOnly ? "Friends Only" : f.Acssesability.ToString()
                })
                .ToList();

            var followersCount = await context.UserFollowings.CountAsync(uf => uf.FolowingId == profileData.UserId);
            var followingCount = await context.UserFollowings.CountAsync(uf => uf.UserId == profileData.UserId);
            var friendsCount = await context.UserFollowings
                .Where(uf => uf.UserId == profileData.UserId)
                .CountAsync(uf => context.UserFollowings.Any(back => back.UserId == uf.FolowingId && back.FolowingId == profileData.UserId));

            var isFollowedByCurrentUser = currentUserId != null && !isOwnProfile && await context.UserFollowings
                .AnyAsync(uf => uf.UserId == currentUserId.Value && uf.FolowingId == profileData.UserId);

            return new ProfileViewModel
            {
                Id = profileData.Id,
                UserId = profileData.UserId,
                ProfilePicture = profileData.ProfilePicture ?? string.Empty,
                Bio = profileData.Bio ?? string.Empty,
                Username = profileData.Username ?? "Unknown user",
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                FriendsCount = friendsCount,
                IsOwnProfile = isOwnProfile,
                IsFollowedByCurrentUser = isFollowedByCurrentUser,
                Folders = visibleFolders,
                NewFolder = new FolderCreateDto { IsCountryFolder = true, Acssesability = Acssesability.Public }
            };
        }
    }
}
