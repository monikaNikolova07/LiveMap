using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Core.Services;
using LiveMap.Data.Models;
using LiveMap.Web.Models.Folder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class FolderController : Controller
    {
        private readonly IFolderService folderService;
        private readonly UserManager<User> userManager;

        public FolderController(IFolderService _folderService, UserManager<User> _userManager)
        {
            this.folderService = _folderService;
            this.userManager = _userManager;
        }

        /*public async Task<IActionResult> Index()
        {
            var folders = await context.Folders
                .Select(f => new FolderViewModel
                {
                    Id = f.Id,
                    Name = f.Name,
                    ProfileId = f.ProfileId,
                    ProfilePicture = f.Profile.ProfilePicture,
                    PicturesCount = f.Pictures.Count,
                    Acssesability = f.Acssesability
                })
                .ToListAsync();

            return View(folders);
        }*/

        public async Task<IActionResult> Index()
        {
            var folders = await folderService.GetAllAsync();
            return View(folders);
        }

        public IActionResult Create()
        {
            ViewBag.Countries = Enum.GetValues(typeof(Country))
                .Cast<Country>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = c.ToString()
                }).ToList();

            return View(new FolderCreateDto());
        }

        /* public IActionResult Create()
        {
            ViewBag.Countries = Enum.GetValues(typeof(Country))
                                .Cast<Country>()
                                .Select(c => new SelectListItem
                                {
                                    Text = c.ToString(),
                                    Value = c.ToString()
                                }).ToList();

            return View(new FolderCreateViewModel());
        }*/


        /*[HttpPost]
        public async Task<IActionResult> Create(FolderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Countries = Enum.GetValues(typeof(Country))
                                        .Cast<Country>()
                                        .Select(c => new SelectListItem
                                        {
                                            Text = c.ToString(),
                                            Value = c.ToString()
                                        }).ToList();

                return View(model);
            }
            var userId = Guid.Parse(userManager.GetUserId(User));
            var profile = context.Users.Include(u => u.Profile).FirstOrDefault(u => u.Id == userId).Profile;
            if (profile == null)
            {
                return NotFound();
            }

            Folder folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = model.Name, 
                ProfileId = profile.Id,
                Acssesability = model.Acssesability
            };

            await context.Folders.AddAsync(folder);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }*/

        [HttpPost]
        public async Task<IActionResult> Create(FolderCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Countries = Enum.GetValues(typeof(Country))
                    .Cast<Country>()
                    .Select(c => new SelectListItem
                    {
                        Text = c.ToString(),
                        Value = c.ToString()
                    }).ToList();

                return View(model);
            }

            var userId = Guid.Parse(userManager.GetUserId(User));

            await folderService.CreateAsync(model, userId);

            return RedirectToAction(nameof(Index));
        }

        /*
        public async Task<IActionResult> Details(Guid id)
        {
            var folder = await context.Folders
                .Include(f => f.Pictures) // взимаме снимките
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null) return NotFound();

            return View(folder);
        }
        */
        /*
       public async Task<IActionResult> Details(Guid id)
       {
           var folder = await folderService.GetByIdAsync(id);
           if (folder == null)
           {
               return NotFound();
           }

           return View(folder);
       }

       // View All Pictures
       public async Task<IActionResult> Pictures(Guid folderId)
       {
           var pictures = await context.Pictures
               .Where(p => p.FolderId == folderId)
               .ToListAsync();

           ViewBag.FolderId = folderId;
           return View(pictures);
       }
       */
        public async Task<IActionResult> Details(Guid id)
        {
            var folder = await folderService.GetByIdAsync(id);
            if (folder == null)
            {
                return NotFound();
            }

            return View(folder);
        }

        /*
        // Upload picture (GET)
        public IActionResult Upload(Guid folderId)
        {
            ViewBag.FolderId = folderId;
            return View();
        }

        // Upload picture (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid folderId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file.");
                ViewBag.FolderId = folderId;
                return View();
            }

            // Създаваме папка wwwroot/uploads, ако няма
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Създаваме уникално име за файла
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Записваме файла на диск
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Създаваме Picture обект с URL
            var picture = new Picture
            {
                Id = Guid.NewGuid(),
                FolderId = folderId,
                URL = "/uploads/" + uniqueFileName,  // тук е пътя за показване в браузъра
                Acssesability = Acssesability.Public  // или задай от форма
            };

            await context.Pictures.AddAsync(picture);
            await context.SaveChangesAsync();

            return RedirectToAction("Pictures", new { folderId });
        }

        

        public async Task<IActionResult> Delete(Guid id)
        {
            var folder = await context.Folders
                .Include(f => f.Pictures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null) return NotFound();

            return View(folder);
        }

        // POST: Folder/Delete/{id} -> изтриване
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var folder = await context.Folders
                .Include(f => f.Pictures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null) return NotFound();

            // Премахваме всички снимки първо
            context.Pictures.RemoveRange(folder.Pictures);

            // Премахваме папката
            context.Folders.Remove(folder);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
        */

        public async Task<IActionResult> Pictures(Guid folderId)
        {
            var pictures = await folderService.GetPicturesAsync(folderId);
            ViewBag.FolderId = folderId;
            return View(pictures);
        }

        public IActionResult Upload(Guid folderId)
        {
            ViewBag.FolderId = folderId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid folderId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a file.");
                ViewBag.FolderId = folderId;
                return View();
            }

            await folderService.UploadPictureAsync(folderId, file);
            return RedirectToAction(nameof(Pictures), new { folderId });
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var folder = await folderService.GetByIdAsync(id);
            if (folder == null)
            {
                return NotFound();
            }

            return View(folder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await folderService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
