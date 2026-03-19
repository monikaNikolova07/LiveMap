using LiveMap.Data.Models;
using LiveMap.Data;
using Microsoft.AspNetCore.Mvc;
using LiveMap.Web.Models.Picture;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LiveMap.Web.Controllers
{
    [Authorize]
    public class PictureController : Controller
    {
        private readonly LiveMapDbContext context;

        public PictureController(LiveMapDbContext context)
        {
            this.context = context;
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
