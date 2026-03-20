using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Profiles;
using LiveMap.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService profileService;

        public ProfileController(IProfileService profileService)
        {
            this.profileService = profileService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var profileDto = await profileService.GetProfileAsync(userId);

            if (profileDto == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Id = profileDto.Id,
                ProfilePicture = profileDto.ProfilePicture,
                Bio = profileDto.Bio,
                Username = profileDto.Username,
                Folders = profileDto.Folders.Select(f => new ProfileFolderViewModel
                {
                    Id = f.Id,
                    Name = f.Name
                }).ToList()
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
        public async Task<IActionResult> Edit(ProfileEditDto dto)
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

            await profileService.EditProfileAsync(dto, userId);

            return RedirectToAction(nameof(Index));
        }
    }
}