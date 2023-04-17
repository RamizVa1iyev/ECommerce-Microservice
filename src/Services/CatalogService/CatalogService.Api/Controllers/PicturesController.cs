using CatalogService.Api.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CatalogService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PicturesController : ControllerBase
    {

        private readonly IWebHostEnvironment _env;
        private readonly CatalogContext _context;
        public PicturesController(IWebHostEnvironment env, CatalogContext context)
        {
            _env = env;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok("App and Runnning");
        }

        [HttpGet]
        [Route("api/v1/catalog/items/{catalogItemId:int}/pic")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetImageAsync(int catalogItemId)
        {
            if (catalogItemId <= 0)
                return BadRequest();

            var item = await _context.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == catalogItemId);

            if (item != null)
            {
                var webRoot = _env.WebRootPath;
                var path = Path.Combine(webRoot, item.PictureFileName);

                string imageFileExtension = Path.GetExtension(item.PictureFileName);
                string mimeType = GetImageMimeTypeFromImageFileExtension(imageFileExtension);

                var buffer = await System.IO.File.ReadAllBytesAsync(path);

                return File(buffer, mimeType);
            }

            return NotFound();
        }

        private string GetImageMimeTypeFromImageFileExtension(string imageFileExtension)
        {
            return imageFileExtension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "",
            };
        }
    }
}
