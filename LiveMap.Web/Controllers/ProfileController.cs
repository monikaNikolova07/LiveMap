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
using LiveMap.Web.Helpers;
using LiveMap.Core.Services;
using Microsoft.AspNetCore.Identity;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService profileService;
        private readonly IImageService imageService;
        private readonly IFolderService folderService;
        private readonly LiveMapDbContext context;
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;

        public ProfileController(
            IProfileService profileService,
            IImageService imageService,
            IFolderService folderService,
            LiveMapDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            this.profileService = profileService;
            this.imageService = imageService;
            this.folderService = folderService;
            this.context = context;
            this.userManager = userManager;
            this.signInManager = signInManager;
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

            if (await IsAdminUserAsync(currentUserId.Value))
            {
                return Forbid();
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
                .Where(p => p.Acssesability == Acssesability.Public)
                .Where(p => !context.UserRoles.Any(ur => ur.UserId == p.UserId && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName)));

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

            if (await IsAdminUserAsync(targetProfile.UserId) || await IsAdminUserAsync(currentUserId.Value))
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

            if (await IsAdminUserAsync(targetProfile.UserId) || await IsAdminUserAsync(currentUserId.Value))
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


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Connections(Guid id, string type)
        {
            var currentUserId = GetCurrentUserId();

            var profile = await context.Profiles
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    Username = p.User.UserName,
                    p.Acssesability
                })
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound();
            }

            var isAdminProfile = await IsAdminUserAsync(profile.UserId);
            var isOwnProfile = currentUserId != null && profile.UserId == currentUserId.Value;
            if (isAdminProfile && !isOwnProfile)
            {
                return NotFound();
            }

            if (!isOwnProfile && profile.Acssesability != Acssesability.Public)
            {
                return NotFound();
            }

            var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            IQueryable<ProfileConnectionItemViewModel> query;
            string title;

            switch (normalizedType)
            {
                case "followers":
                    title = "Followers";
                    query = context.UserFollowings
                        .Where(uf => uf.FolowingId == profile.UserId)
                        .Select(uf => new ProfileConnectionItemViewModel
                        {
                            ProfileId = uf.User.Profile != null ? uf.User.Profile.Id : Guid.Empty,
                            UserId = uf.UserId,
                            Username = uf.User.UserName ?? "Unknown user",
                            ProfilePicture = uf.User.Profile != null ? uf.User.Profile.ProfilePicture : null,
                            Bio = uf.User.Profile != null ? uf.User.Profile.Bio : null
                        });
                    break;

                case "following":
                    title = "Following";
                    query = context.UserFollowings
                        .Where(uf => uf.UserId == profile.UserId)
                        .Select(uf => new ProfileConnectionItemViewModel
                        {
                            ProfileId = uf.Following.Profile != null ? uf.Following.Profile.Id : Guid.Empty,
                            UserId = uf.FolowingId,
                            Username = uf.Following.UserName ?? "Unknown user",
                            ProfilePicture = uf.Following.Profile != null ? uf.Following.Profile.ProfilePicture : null,
                            Bio = uf.Following.Profile != null ? uf.Following.Profile.Bio : null
                        });
                    break;

                case "friends":
                    title = "Friends";
                    query = context.UserFollowings
                        .Where(uf => uf.UserId == profile.UserId)
                        .Where(uf => context.UserFollowings.Any(back => back.UserId == uf.FolowingId && back.FolowingId == profile.UserId))
                        .Select(uf => new ProfileConnectionItemViewModel
                        {
                            ProfileId = uf.Following.Profile != null ? uf.Following.Profile.Id : Guid.Empty,
                            UserId = uf.FolowingId,
                            Username = uf.Following.UserName ?? "Unknown user",
                            ProfilePicture = uf.Following.Profile != null ? uf.Following.Profile.ProfilePicture : null,
                            Bio = uf.Following.Profile != null ? uf.Following.Profile.Bio : null
                        });
                    break;

                default:
                    return NotFound();
            }

            var items = await query
                .Where(x => x.ProfileId != Guid.Empty)
                .OrderBy(x => x.Username)
                .ToListAsync();

            var model = new ProfileConnectionsViewModel
            {
                ProfileId = profile.Id,
                ProfileUsername = profile.Username ?? "Unknown user",
                ConnectionType = normalizedType,
                Title = title,
                Items = items
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && await IsAdminUserAsync(currentUserId.Value))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var model = new NotificationsViewModel
            {
                Followers = await context.UserFollowings
                    .Where(uf => uf.FolowingId == currentUserId.Value)
                    .Where(uf => !context.UserRoles.Any(ur => ur.UserId == uf.UserId && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName)))
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditDto dto, IFormFile? imageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var normalizedRequestedUsername = dto.Username.Trim();
            var existingUser = await userManager.FindByNameAsync(normalizedRequestedUsername);
            if (existingUser != null && existingUser.Id != currentUser.Id)
            {
                ModelState.AddModelError(nameof(dto.Username), "This username is already taken.");
                return View(dto);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var imageResult = await imageService.UploadImageAsync(
                        imageFile,
                        Guid.NewGuid().ToString(),
                        "profiles");

                    dto.ProfilePicture = imageResult.Url;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(dto);
                }
            }

            dto.Username = normalizedRequestedUsername;

            if (!string.Equals(currentUser.UserName, dto.Username, StringComparison.Ordinal))
            {
                currentUser.UserName = dto.Username;
                currentUser.NormalizedUserName = dto.Username.ToUpperInvariant();

                var updateResult = await userManager.UpdateAsync(currentUser);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(dto);
                }
            }

            await profileService.EditProfileAsync(dto, userId);
            await signInManager.RefreshSignInAsync(currentUser);

            TempData["SuccessMessage"] = "Profile updated successfully.";
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
            var currentUserIsAdmin = currentUserId.HasValue && await IsAdminUserAsync(currentUserId.Value);
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

            var isAdminProfile = await IsAdminUserAsync(profileData.UserId);
            var isOwnProfile = currentUserId != null && profileData.UserId == currentUserId.Value;

            if (isAdminProfile && !isOwnProfile)
            {
                return null;
            }

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
                .Where(f => !isAdminProfile && (isOwnProfile
                    || f.Acssesability == Acssesability.Public
                    || (f.Acssesability == Acssesability.FriendsOnly && areFriends)))
                .Select(f =>
                {
                    var isCountryFolder = CountryUiHelper.TryParseCountry(f.Name, out var country);
                    return new ProfileFolderViewModel
                    {
                        Id = f.Id,
                        Name = f.Name,
                        AccessibilityLabel = f.Acssesability == Acssesability.FriendsOnly ? "Friends Only" : f.Acssesability.ToString(),
                        IsCountryFolder = isCountryFolder,
                        FlagEmoji = isCountryFolder ? CountryUiHelper.GetFlagEmoji(country) : "📁",
                        FlagPaletteStyle = isCountryFolder ? CountryUiHelper.GetFlagPaletteStyle(country) : string.Empty
                    };
                })
                .ToList();

            var followersCount = isAdminProfile ? 0 : await context.UserFollowings.CountAsync(uf => uf.FolowingId == profileData.UserId);
            var followingCount = isAdminProfile ? 0 : await context.UserFollowings.CountAsync(uf => uf.UserId == profileData.UserId);
            var friendsCount = isAdminProfile ? 0 : await context.UserFollowings
                .Where(uf => uf.UserId == profileData.UserId)
                .CountAsync(uf => context.UserFollowings.Any(back => back.UserId == uf.FolowingId && back.FolowingId == profileData.UserId));

            var isFollowedByCurrentUser = !isAdminProfile && !currentUserIsAdmin && currentUserId != null && !isOwnProfile && await context.UserFollowings
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
                IsAdminProfile = isAdminProfile,
                Folders = visibleFolders,
                NewFolder = new FolderCreateDto { IsCountryFolder = true, Acssesability = Acssesability.Public }
            };
        }

        private Task<bool> IsAdminUserAsync(Guid userId)
        {
            return context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName));
        }
    }
}
