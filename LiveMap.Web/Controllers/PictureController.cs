using LiveMap.Data.Models;
using LiveMap.Data;
using Microsoft.AspNetCore.Mvc;
using LiveMap.Web.Models.Picture;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LiveMap.Core.Contracts;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class PictureController : Controller
    {
        private readonly LiveMapDbContext context;
        private readonly IFolderService folderService;

        public PictureController(LiveMapDbContext context, IFolderService folderService)
        {
            this.context = context;
            this.folderService = folderService;
        }

        public async Task<IActionResult> Index()
        {
            var pictures = await context.Pictures
                .Select(p => new PictureViewModel
                {
                    Id = p.Id,
                    URL = p.URL,
                    FolderId = p.FolderId,
                    FolderName = p.Folder.Name,
                    Acssesability = p.Acssesability
                })
                .ToListAsync();

            return View(pictures);
        }


        public async Task<IActionResult> EditVisibility(Guid id)
        {
            var picture = await context.Pictures
                .Include(p => p.Folder)
                    .ThenInclude(f => f.Profile)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (picture == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || picture.Folder.Profile.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            await PopulateAccessibilitiesAsync(picture.FolderId);

            var model = new PictureEditVisibilityViewModel
            {
                Id = picture.Id,
                FolderId = picture.FolderId,
                Url = picture.URL,
                Acssesability = await folderService.GetUploadAccessibilityAsync(picture.FolderId, picture.Acssesability)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVisibility(PictureEditVisibilityViewModel model)
        {
            var picture = await context.Pictures
                .Include(p => p.Folder)
                    .ThenInclude(f => f.Profile)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (picture == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || picture.Folder.Profile.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.Url = picture.URL;
                model.FolderId = picture.FolderId;
                await PopulateAccessibilitiesAsync(picture.FolderId);
                return View(model);
            }

            picture.Acssesability = await folderService.GetUploadAccessibilityAsync(picture.FolderId, model.Acssesability);
            await context.SaveChangesAsync();

            return RedirectToAction("Details", "Folder", new { id = picture.FolderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid folderId)
        {
            var picture = await context.Pictures
                .Include(p => p.Folder)
                    .ThenInclude(f => f.Profile)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (picture == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || picture.Folder.Profile.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            context.Pictures.Remove(picture);
            await context.SaveChangesAsync();

            return RedirectToAction("Details", "Folder", new { id = folderId == Guid.Empty ? picture.FolderId : folderId });
        }

        private async Task PopulateAccessibilitiesAsync(Guid folderId)
        {
            ViewBag.Accessibilities = (await folderService.GetAvailableAccessibilitiesForCreateAsync(folderId))
                .Select(a => new SelectListItem
                {
                    Text = a == Acssesability.FriendsOnly ? "Friends Only" : a.ToString(),
                    Value = a.ToString()
                }).ToList();
        }

        private Guid? GetCurrentUserId()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PictureCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Picture picture = new Picture
            {
                Id = Guid.NewGuid(),
                URL = model.URL,
                FolderId = model.FolderId,
                Acssesability = model.Acssesability
            };

            await context.Pictures.AddAsync(picture);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
