using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Data;
using LiveMap.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class FolderController : Controller
    {
        private readonly IFolderService folderService;
        private readonly UserManager<User> userManager;
        private readonly IImageService imageService;
        private readonly LiveMapDbContext context;

        public FolderController(
     IFolderService folderService,
     UserManager<User> userManager,
     IImageService imageService,
     LiveMapDbContext context)
        {
            this.folderService = folderService;
            this.userManager = userManager;
            this.imageService = imageService;
            this.context = context;
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
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                ViewBag.Errors = errors;

                ViewBag.Countries = Enum.GetValues(typeof(Country))
                    .Cast<Country>()
                    .Select(c => new SelectListItem
                    {
                        Text = c.ToString(),
                        Value = c.ToString()
                    }).ToList();

                return View(model);
            }

            var userIdString = userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userIdString))
            {
                return Content("UserId is null");
            }

            var userId = Guid.Parse(userIdString);

            try
            {
                await folderService.CreateAsync(model, userId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
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

            try
            {
                var imageResult = await imageService.UploadImageAsync(
                    file,
                    Guid.NewGuid().ToString(),
                    "images");

                var picture = new Picture
                {
                    Id = Guid.NewGuid(),
                    URL = imageResult.Url,
                    FolderId = folderId,
                    Acssesability = Acssesability.Public,
                    CreatedOn = DateTime.UtcNow
                };

                await context.Pictures.AddAsync(picture);
                await context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = folderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.FolderId = folderId;
                return View();
            }
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
