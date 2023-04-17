using CatalogService.Api.Core.Application;
using CatalogService.Api.Core.Domain;
using CatalogService.Api.Infrastructure.Context;
using CatalogService.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly CatalogContext _context;
        private readonly CatalogSettings _settings;

        public CatalogController(CatalogContext context, IOptionsSnapshot<CatalogSettings> settings)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settings = settings.Value;

            context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        }

        [HttpGet]
        [Route("items")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(IEnumerable<CatalogItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ItemsAsync([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, string ids = "")
        {
            if (!String.IsNullOrEmpty(ids))
            {
                var items = await GetItemsByIdAsync(ids);

                if (!items.Any())
                {
                    return BadRequest("Ids value invalid. Must be comma-separated list of numbers ");
                }
                return Ok(items);
            }

            var totalItems = await _context.CatalogItems.LongCountAsync();
            var itemsOnPage = await _context.CatalogItems
                .OrderBy(c => c.Name)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToListAsync();

            itemsOnPage = ChangeUriPlaceHolder(itemsOnPage);
            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

            return Ok(model);
        }

        private async Task<List<CatalogItem>> GetItemsByIdAsync(string ids)
        {
            var numIds = ids.Split(',').Select(id => (Ok: int.TryParse(id, out int x), Value: x));
            if (!numIds.All(nid => nid.Ok))
            {
                return new List<CatalogItem>();
            }

            var idsToSelect = numIds.Select(id => id.Value);
            var items = await _context.CatalogItems.Where(ci => idsToSelect.Contains(ci.Id)).ToListAsync();
            items = ChangeUriPlaceHolder(items);

            return items;
        }

        [HttpGet]
        [Route("items/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(CatalogItem), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CatalogItem>> ItembyIdAsync(int id)
        {
            if (id <= 0)
                return BadRequest();

            var item = await _context.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == id);

            var baseUri = _settings.PicBaseUrl;
            if (item != null)
            {
                item.PictureUrl = baseUri + item.PictureFileName;
                return item;
            }
            return NotFound();

        }

        [HttpGet]
        [Route("items/withname/{name}")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ItemsWithNameAsync(string name, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
        {
            var totalItems = await _context.CatalogItems.Where(ci => ci.Name.StartsWith(name)).LongCountAsync();
            var itemsOnPage = await _context.CatalogItems
                .Where(ci => ci.Name.StartsWith(name))
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToListAsync();

            itemsOnPage = ChangeUriPlaceHolder(itemsOnPage);
            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

            return Ok(model);
        }

        [Route("items")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult> UpdateProductAsync([FromBody] CatalogItem productToUpdate)
        {
            var catalogItem = await _context.CatalogItems.SingleOrDefaultAsync(c => c.Id == productToUpdate.Id);
            if (catalogItem == null)
                return NotFound(new { Message = $" Item with id {productToUpdate.Id} not found" });

            var oldPrice = catalogItem.Price;
            var raiseProductPriceChangedEvent = oldPrice != productToUpdate.Price;
            catalogItem = productToUpdate;
            _context.CatalogItems.Update(catalogItem);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ItembyIdAsync), new { id = productToUpdate.Id }, null);
        }
        [Route("items")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult> CreateProductAsync([FromBody] CatalogItem product)
        {
            var item = new CatalogItem
            {
                CatalogBrandId = product.CatalogBrandId,
                CatalogTypeId = product.CatalogTypeId,
                Description = product.Description,
                Price = product.Price,
                Name = product.Name,
                PictureFileName = product.PictureFileName,
            };

            _context.CatalogItems.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ItembyIdAsync), new { id = item.Id }, null);
        }

        [Route("{id}")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> DeleteProductAsync(int id)
        {
            var catalogItem = await _context.CatalogItems.SingleOrDefaultAsync(c => c.Id == id);
            if (catalogItem == null)
                return NotFound();
            _context.CatalogItems.Remove(catalogItem);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private List<CatalogItem> ChangeUriPlaceHolder(List<CatalogItem> items)
        {
            var baseUri = _settings.PicBaseUrl;
            foreach (var item in items)
            {
                if (item != null)
                    item.PictureUrl = baseUri + item.PictureFileName;
            }
            return items;
        }
    }
}
