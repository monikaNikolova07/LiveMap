using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Web.Models.Folder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LiveMap.Core.Services;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class FolderController : Controller
    {
        private readonly IFolderService folderService;
        private readonly UserManager<User> userManager;
        private readonly LiveMapDbContext context;

        public FolderController(
            IFolderService folderService,
            UserManager<User> userManager,
            LiveMapDbContext context)
        {
            this.folderService = folderService;
            this.userManager = userManager;
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            var folders = await folderService.GetAllAsync(currentUserId);
            return View(folders);
        }

        public async Task<IActionResult> Create(Guid? parentFolderId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || await IsAdminUserAsync(currentUserId.Value))
            {
                return Forbid();
            }

            await PopulateCreateViewDataAsync(parentFolderId, !parentFolderId.HasValue);
            return View(new FolderCreateDto { ParentFolderId = parentFolderId, IsCountryFolder = !parentFolderId.HasValue });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FolderCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCreateViewDataAsync(model.ParentFolderId, !model.ParentFolderId.HasValue);
                return View(model);
            }

            var userIdString = userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdString);
            if (await IsAdminUserAsync(userId))
            {
                return Forbid();
            }

            try
            {
                var createdFolder = await folderService.CreateAsync(model, userId);
                return RedirectToAction(nameof(Details), new { id = createdFolder.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateCreateViewDataAsync(model.ParentFolderId, !model.ParentFolderId.HasValue);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var folder = await folderService.GetByIdAsync(id);
            if (folder == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            var canView = await CanViewFolderAsync(folder, currentUserId);
            if (!canView)
            {
                return Forbid();
            }

            var isOwner = currentUserId.HasValue && folder.Profile.UserId == currentUserId.Value;
            var currentUserIsAdmin = currentUserId.HasValue && await IsAdminUserAsync(currentUserId.Value);
            var canSeeFriendsOnly = isOwner || await IsFriendAsync(folder.Profile.UserId, currentUserId);

            var model = new FolderDetailsViewModel
            {
                Id = folder.Id,
                Name = folder.Name,
                Acssesability = folder.Acssesability,
                IsOwner = isOwner,
                OwnerProfileId = folder.ProfileId,
                ParentFolderId = folder.ParentFolders.Select(pf => (Guid?)pf.FolderId).FirstOrDefault(),
                Pictures = folder.Pictures
                    .Where(p => CanViewByAccessibility(p.Acssesability, isOwner, canSeeFriendsOnly))
                    .OrderByDescending(p => p.CreatedOn)
                    .Select(p => new FolderPictureItemViewModel
                    {
                        Id = p.Id,
                        Url = p.URL,
                        Acssesability = p.Acssesability,
                        Description = p.Description,
                        LikesCount = p.Likes.Count,
                        CommentsCount = p.Comments.Count,
                        IsLikedByCurrentUser = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value),
                        CanInteract = currentUserId.HasValue && !currentUserIsAdmin && !isOwner,
                        Comments = p.Comments
                            .OrderByDescending(c => c.CreatedOn)
                            .Take(5)
                            .Select(c => new PictureCommentItemViewModel
                            {
                                Id = c.Id,
                                Username = c.User.UserName ?? "Unknown user",
                                Content = c.Content,
                                CreatedOn = c.CreatedOn
                            })
                            .ToList()
                    })
                    .ToList(),
                Subfolders = folder.Subfolders
                    .Select(fs => fs.Subfolder)
                    .Where(sf => CanViewByAccessibility(sf.Acssesability, isOwner, canSeeFriendsOnly))
                    .OrderBy(sf => sf.Name)
                    .Select(sf => new FolderChildItemViewModel
                    {
                        Id = sf.Id,
                        Name = sf.Name,
                        Acssesability = sf.Acssesability,
                        PicturesCount = sf.Pictures.Count
                    })
                    .ToList(),
                CreateSubfolder = new FolderCreateDto
                {
                    ParentFolderId = folder.Id,
                    IsCountryFolder = false,
                    Acssesability = folder.Acssesability == Acssesability.Private ? Acssesability.Private : Acssesability.Public
                },
                UploadPicture = new FolderPictureUploadViewModel
                {
                    FolderId = folder.Id,
                    Acssesability = folder.Acssesability == Acssesability.Private ? Acssesability.Private : Acssesability.Public
                }
            };

            return View(model);
        }

        public async Task<IActionResult> Upload(Guid folderId)
        {
            var folder = await folderService.GetByIdAsync(folderId);
            if (folder == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || folder.Profile.UserId != currentUserId.Value || await IsAdminUserAsync(currentUserId.Value))
            {
                return Forbid();
            }

            var effectiveAccessibility = await folderService.GetUploadAccessibilityAsync(folderId, Acssesability.Public);
            var model = new FolderPictureUploadViewModel { FolderId = folderId, Acssesability = effectiveAccessibility };
            ViewBag.Accessibilities = (await folderService.GetAvailableAccessibilitiesForCreateAsync(folderId))
                .Select(a => new SelectListItem
                {
                    Text = a == Acssesability.FriendsOnly ? "Friends Only" : a.ToString(),
                    Value = a.ToString()
                }).ToList();
            ViewBag.CurrentFolderId = folderId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(FolderPictureUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Accessibilities = (await folderService.GetAvailableAccessibilitiesForCreateAsync(model.FolderId))
                    .Select(a => new SelectListItem
                    {
                        Text = a == Acssesability.FriendsOnly ? "Friends Only" : a.ToString(),
                        Value = a.ToString()
                    }).ToList();
                ViewBag.CurrentFolderId = model.FolderId;
                return View(model);
            }

            var folder = await folderService.GetByIdAsync(model.FolderId);
            var currentUserId = GetCurrentUserId();
            if (folder == null || currentUserId == null || folder.Profile.UserId != currentUserId.Value || await IsAdminUserAsync(currentUserId.Value))
            {
                return Forbid();
            }

            try
            {
                await folderService.UploadPictureAsync(model.FolderId, model.File!, model.Acssesability, model.Description);
                return RedirectToAction(nameof(Details), new { id = model.FolderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Accessibilities = (await folderService.GetAvailableAccessibilitiesForCreateAsync(model.FolderId))
                    .Select(a => new SelectListItem
                    {
                        Text = a == Acssesability.FriendsOnly ? "Friends Only" : a.ToString(),
                        Value = a.ToString()
                    }).ToList();
                ViewBag.CurrentFolderId = model.FolderId;
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var folder = await folderService.GetByIdAsync(id);
            if (folder == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || folder.Profile.UserId != currentUserId.Value || await IsAdminUserAsync(currentUserId.Value))
            {
                return Forbid();
            }

            var parentFolderId = folder.ParentFolders.Select(pf => (Guid?)pf.FolderId).FirstOrDefault();
            await PopulateCreateViewDataAsync(parentFolderId, !parentFolderId.HasValue);

            var model = new FolderEditViewModel
            {
                Id = folder.Id,
                Name = folder.Name,
                Acssesability = await folderService.GetUploadAccessibilityAsync(folder.Id, folder.Acssesability),
                ParentFolderId = parentFolderId,
                ProfileId = folder.ProfileId,
                IsCountryFolder = !parentFolderId.HasValue
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FolderEditViewModel model)
        {
            var folder = await folderService.GetByIdAsync(model.Id);
            if (folder == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || folder.Profile.UserId != currentUserId.Value || await IsAdminUserAsync(currentUserId.Value))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewDataAsync(model.ParentFolderId, model.IsCountryFolder);
                return View(model);
            }

            try
            {
                await folderService.UpdateAsync(model.Id, model.Name, model.Acssesability);
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateCreateViewDataAsync(model.ParentFolderId, model.IsCountryFolder);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, string? returnUrl = null)
        {
            var folder = await folderService.GetByIdAsync(id);
            var currentUserId = GetCurrentUserId();
            if (folder == null || currentUserId == null || folder.Profile.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            var parentFolderId = folder.ParentFolders.Select(pf => (Guid?)pf.FolderId).FirstOrDefault();
            var profileId = folder.ProfileId;

            await folderService.DeleteAsync(id);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (parentFolderId.HasValue)
            {
                return RedirectToAction(nameof(Details), new { id = parentFolderId.Value });
            }

            return RedirectToAction("Index", "Profile", new { id = profileId });
        }

        private async Task PopulateCreateViewDataAsync(Guid? parentFolderId, bool isCountryFolder)
        {
            ViewBag.Countries = Enum.GetValues(typeof(Country))
                .Cast<Country>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = c.ToString()
                }).ToList();

            var accessibilities = await folderService.GetAvailableAccessibilitiesForCreateAsync(parentFolderId);
            ViewBag.Accessibilities = accessibilities
                .Select(a => new SelectListItem
                {
                    Text = a switch
                    {
                        Acssesability.FriendsOnly => "Friends Only",
                        _ => a.ToString()
                    },
                    Value = a.ToString()
                }).ToList();

            ViewBag.ParentFolderId = parentFolderId;
            ViewBag.IsCountryFolder = isCountryFolder;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdString = userManager.GetUserId(User);
            return Guid.TryParse(userIdString, out var userId) ? userId : null;
        }

        private async Task<bool> CanViewFolderAsync(Folder folder, Guid? currentUserId)
        {
            var isOwner = currentUserId.HasValue && folder.Profile.UserId == currentUserId.Value;
            if (await IsAdminUserAsync(folder.Profile.UserId) && !isOwner)
            {
                return false;
            }
            if (isOwner)
            {
                return true;
            }

            if (folder.Acssesability == Acssesability.Public)
            {
                return true;
            }

            if (folder.Acssesability == Acssesability.Private)
            {
                return false;
            }

            return await IsFriendAsync(folder.Profile.UserId, currentUserId);
        }

        private Task<bool> IsAdminUserAsync(Guid userId)
        {
            return context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdminService.AdminRoleName));
        }

        private async Task<bool> IsFriendAsync(Guid ownerUserId, Guid? currentUserId)
        {
            if (!currentUserId.HasValue)
            {
                return false;
            }

            return await context.UserFollowings.AnyAsync(uf => uf.UserId == currentUserId.Value && uf.FolowingId == ownerUserId)
                   && await context.UserFollowings.AnyAsync(uf => uf.UserId == ownerUserId && uf.FolowingId == currentUserId.Value);
        }

        private static bool CanViewByAccessibility(Acssesability acssesability, bool isOwner, bool canSeeFriendsOnly)
        {
            if (isOwner)
            {
                return true;
            }

            return acssesability switch
            {
                Acssesability.Public => true,
                Acssesability.FriendsOnly => canSeeFriendsOnly,
                _ => false
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreateSubfolder(Guid parentFolderId)
        {
            await PopulateCreateViewDataAsync(parentFolderId, false);

            var model = new FolderCreateDto
            {
                ParentFolderId = parentFolderId,
                IsCountryFolder = false
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubfolder(FolderCreateDto model)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);

            model.IsCountryFolder = false;

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewDataAsync(model.ParentFolderId, false);
                return View(model);
            }

            var createdFolder = await folderService.CreateAsync(model, userId);

            return RedirectToAction("Details", new { id = createdFolder.Id });
        }
    }
}
