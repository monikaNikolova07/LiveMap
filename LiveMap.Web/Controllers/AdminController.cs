using LiveMap.Core.Contracts;
using LiveMap.Core.Services;
using LiveMap.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveMap.Web.Controllers
{
    [Authorize(Roles = AdminService.AdminRoleName)]
    public class AdminController : Controller
    {
        private readonly IAdminService adminService;
        private readonly ILogger<AdminController> logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            this.adminService = adminService;
            this.logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await adminService.GetDashboardAsync();
            return View(new AdminDashboardViewModel
            {
                Profiles = dashboard.Profiles,
                Folders = dashboard.Folders,
                Photos = dashboard.Photos
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile(Guid id)
        {
            try
            {
                await adminService.DeleteProfileAsync(id);
                TempData["AdminSuccess"] = "Profile deleted successfully.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete profile {ProfileId}", id);
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFolder(Guid id)
        {
            try
            {
                await adminService.DeleteFolderAsync(id);
                TempData["AdminSuccess"] = "Folder deleted successfully.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete folder {FolderId}", id);
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            try
            {
                await adminService.DeletePhotoAsync(id);
                TempData["AdminSuccess"] = "Photo deleted successfully.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete photo {PhotoId}", id);
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfilePicture(Guid id)
        {
            try
            {
                await adminService.DeleteProfilePictureAsync(id);
                TempData["AdminSuccess"] = "Profile picture deleted successfully.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete profile picture for profile {ProfileId}", id);
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
