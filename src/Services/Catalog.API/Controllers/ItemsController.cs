using Catalog.API.Data;
using Catalog.API.Integration.ItemEvents;
using Catalog.API.Model;
using Catalog.API.Model.CatalogItemDTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;


[Route("api/[controller]")]
[ApiController]
public class ItemsController : ControllerBase
{
    private readonly CatalogContext _context;
    private readonly IEventsPublisher _eventsPublisher;

    public ItemsController(CatalogContext context, IEventsPublisher eventsPublisher)
    {
        _context = context;
        _eventsPublisher = eventsPublisher;
    }

    // GET: api/CatalogItem
    [HttpGet]
    public async Task<ActionResult<PaginatedItemsDTO<CatalogItemDTO>>>
    GetCatalogItems(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 0)
    {
        var items = _context.CatalogItems
            .Include(item => item.CatalogBrand)
            .Include(item => item.CatalogType)
            .Select(item => new CatalogItemDTO
            {
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                CatalogType = item.CatalogType!.Type,
                CatalogBrand = item.CatalogBrand!.Brand,
                AvailableStock = item.AvailableStock,
                RestockThreshold = item.RestockThreshold,
                MaxStockThreshold = item.MaxStockThreshold,
                OnReorder = item.OnReorder
            })
            .OrderBy(item => item.Name);

        var totalItems = await items.LongCountAsync();

        var itemsOnPage = await items
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        var model =
            new PaginatedItemsDTO<CatalogItemDTO>(
                pageIndex,
                pageSize,
                totalItems,
                itemsOnPage);

        return Ok(model);
    }

    // GET: api/CatalogItem/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogItemDTO>>
    GetCatalogItem(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var catalogItem = await _context.CatalogItems
            .Include(item => item.CatalogBrand)
            .Include(item => item.CatalogType)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (catalogItem == null)
        {
            return NotFound();
        }

        var catalogItemDTO = new CatalogItemDTO
        {
            Name = catalogItem.Name,
            Description = catalogItem.Description,
            Price = catalogItem.Price,
            CatalogType = catalogItem.CatalogType!.Type,
            CatalogBrand = catalogItem.CatalogBrand!.Brand,
            AvailableStock = catalogItem.AvailableStock,
            RestockThreshold = catalogItem.RestockThreshold,
            MaxStockThreshold = catalogItem.MaxStockThreshold,
            OnReorder = catalogItem.OnReorder
        };

        return catalogItemDTO;
    }

    // PUT: api/CatalogItem/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut]
    [ProducesResponseType(typeof(CatalogItemDTO), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CatalogItemDTO>> PutCatalogItem(
    CatalogItemDTO itemDTO)
    {
        bool priceChanged = false;

        if (itemDTO.Name == null)
            return BadRequest();

        CatalogItem? item = await _context.CatalogItems
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .FirstOrDefaultAsync(i => i.Name == itemDTO.Name);

        if (item == null)
            return NotFound(
                $"Item {itemDTO.Name} does not exist in the catalog");

        if (item.Price != itemDTO.Price)
        {
            priceChanged = true;
        }

        if (item.CatalogType.Type != itemDTO.CatalogType)
        {
            var type = _context.CatalogTypes
                .FirstOrDefault(t => t.Type == itemDTO.CatalogType);

            if (type == null)
            {
                item.CatalogType = new CatalogType
                {
                    Type = itemDTO.CatalogType
                };
            }
            else
            {
                item.CatalogType = type;
            }
        }

        if (item.CatalogBrand.Brand != itemDTO.CatalogBrand)
        {
            var brand = _context.CatalogBrands
                .FirstOrDefault(b => b.Brand == itemDTO.CatalogBrand);

            if (brand == null)
            {
                item.CatalogBrand = new CatalogBrand
                {
                    Brand = itemDTO.CatalogBrand
                };
            }
            else
            {
                item.CatalogBrand = brand;
            }
        }

        item.MaxStockThreshold = itemDTO.MaxStockThreshold;
        item.Name = itemDTO.Name;
        item.OnReorder = itemDTO.OnReorder;
        item.Price = itemDTO.Price;
        item.RestockThreshold = itemDTO.RestockThreshold;
        item.AvailableStock = itemDTO.AvailableStock;
        item.Description = itemDTO.Description;

        _context.Entry(item).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();

            if (priceChanged)
            {
                var itemPriceChangedEvent =
                    new ItemPriceChangedEvent
                    {
                        Name = item.Name,
                        Price = item.Price
                    };

                await _eventsPublisher.Publish(
                    itemPriceChangedEvent);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }

        return Ok(itemDTO);
    }

    // POST: api/CatalogItem
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<CatalogItemDTO>>
    PostCatalogItem(CatalogItemDTO catalogItemDTO)
    {
        var catalogItem = await _context.CatalogItems
            .FirstOrDefaultAsync(
                c => c.Name == catalogItemDTO.Name);

        if (catalogItem != null)
        {
            return BadRequest(
                "Product item must be unique");
        }

        catalogItem = new CatalogItem
        {
            Name = catalogItemDTO.Name,
            Description = catalogItemDTO.Description,
            Price = catalogItemDTO.Price,
            AvailableStock = catalogItemDTO.AvailableStock,
            RestockThreshold = catalogItemDTO.RestockThreshold,
            MaxStockThreshold = catalogItemDTO.MaxStockThreshold,
            OnReorder = catalogItemDTO.OnReorder
        };

        var catalogType = await _context.CatalogTypes
            .FirstOrDefaultAsync(
                c => c.Type == catalogItemDTO.CatalogType);

        if (catalogType == null)
        {
            catalogType = new CatalogType
            {
                Type = catalogItemDTO.CatalogType
            };

            _context.CatalogTypes.Add(catalogType);
        }

        var catalogBrand = await _context.CatalogBrands
            .FirstOrDefaultAsync(
                c => c.Brand == catalogItemDTO.CatalogBrand);

        if (catalogBrand == null)
        {
            catalogBrand = new CatalogBrand
            {
                Brand = catalogItemDTO.CatalogBrand
            };

            _context.CatalogBrands.Add(catalogBrand);
        }

        catalogItem.CatalogType = catalogType;
        catalogItem.CatalogBrand = catalogBrand;

        _context.CatalogItems.Add(catalogItem);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCatalogItem),
            new { id = catalogItem.Id },
            catalogItemDTO);
    }

    // DELETE: api/CatalogItem/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCatalogItem(int? id)
    {
        var catalogitem = await _context.CatalogItems.FindAsync(id);
        if (catalogitem == null)
        {
            return NotFound();
        }

        _context.CatalogItems.Remove(catalogitem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CatalogItemExists(int? id)
    {
        return _context.CatalogItems.Any(e => e.Id == id);
    }
}
