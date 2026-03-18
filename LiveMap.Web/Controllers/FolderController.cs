using LiveMap.Data;
using LiveMap.Data.Models;
using LiveMap.Web.Models.Folder;
using LiveMap.Web.Models.Folder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Web.Controllers
{
    public class FolderController : Controller
    {
        private readonly LiveMapDbContext context;

        public FolderController(LiveMapDbContext _context)
        {
            context = _context;
        }

        /*public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Folder folder)
        {
            if (ModelState.IsValid)
            {
                folder.Id = Guid.NewGuid();
                context.Folders.Add(folder);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(folder);
        }

        public async Task<IActionResult> Index()
        {
            var folders = await context.Folders
                .Include(f => f.Profile)
                .Include(f => f.Pictures)
                .ToListAsync();

            return View(folders);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var folder = await context.Folders
                .Include(f => f.Pictures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
                return NotFound();

            return View(folder);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var folder = await context.Folders.FindAsync(id);
            if (folder == null)
                return NotFound();

            return View(folder);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, Folder folder)
        {
            if (id != folder.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                context.Update(folder);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(folder);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var folder = await context.Folders.FindAsync(id);
            if (folder == null)
                return NotFound();

            return View(folder);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var folder = await context.Folders.FindAsync(id);
            context.Folders.Remove(folder);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
        */

        public async Task<IActionResult> Index()
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
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(FolderCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Folder folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                ProfileId = model.ProfileId,
                Acssesability = model.Acssesability
            };

            await context.Folders.AddAsync(folder);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var folder = await context.Folders.FindAsync(id);

            if (folder == null)
                return NotFound();

            context.Folders.Remove(folder);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
