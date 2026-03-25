using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Core.DTOs.Profiles;
using LiveMap.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LiveMap.Data;
using LiveMap.Data.Models;
using System.Security.Claims;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService profileService;
        private readonly IImageService imageService;
        private readonly LiveMapDbContext context;

        public ProfileController(IProfileService profileService, IImageService imageService, LiveMapDbContext context)
        {
            this.profileService = profileService;
            this.imageService = imageService;
            this.context = context;
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

            ViewBag.Countries = Enum.GetValues(typeof(Country))
                .Cast<Country>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = c.ToString()
                }).ToList();

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

        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var profile = await context.Profiles
                .Where(p => p.Id == id && p.Acssesability == LiveMap.Data.Models.Acssesability.Public)
                .Select(p => new ProfileViewModel
                {
                    Id = p.Id,
                    ProfilePicture = p.ProfilePicture,
                    Bio = p.Bio,
                    Username = p.User.UserName,
                    Folders = p.Folders
                        .Where(f => f.Acssesability == LiveMap.Data.Models.Acssesability.Public)
                        .Select(f => new ProfileFolderViewModel
                        {
                            Id = f.Id,
                            Name = f.Name
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound();
            }

            ViewBag.IsPublicProfile = true;
            return View("Index", profile);
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

            dto.Username = (dto.Username ?? string.Empty).Trim();

            if (await context.Users.AnyAsync(u => u.UserName != null &&
                                                  u.Id.ToString() != userId &&
                                                  u.UserName.ToLower() == dto.Username.ToLower()))
            {
                ModelState.AddModelError(nameof(dto.Username), "This username is already taken.");
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
    }
}
