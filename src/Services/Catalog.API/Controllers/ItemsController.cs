using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Model;
using Catalog.API.Data;
using Catalog.API.Model.CatalogItemDTO;


[Route("api/[controller]")]
[ApiController]
public class ItemsController : ControllerBase
{
    private readonly CatalogContext _context;
    public ItemsController(CatalogContext context)
    {
        _context = context;
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
    [HttpPut("{id}")]
    public async Task<IActionResult>
    PutCatalogItem(
        int id,
        CatalogItemDTO catalogItemDTO)
    {
        var catalogItem =
            await _context.CatalogItems
                .Include(c => c.CatalogType)
                .Include(c => c.CatalogBrand)
                .FirstOrDefaultAsync(c => c.Id == id);

        if (catalogItem == null)
        {
            return NotFound(
                "Item does not exist in the catalog");
        }

        var duplicate =
            await _context.CatalogItems
                .FirstOrDefaultAsync(
                    c => c.Name == catalogItemDTO.Name
                         && c.Id != id);

        if (duplicate != null)
        {
            return BadRequest(
                "Product item must be unique");
        }

        var type =
            await _context.CatalogTypes
                .FirstOrDefaultAsync(
                    c => c.Type == catalogItemDTO.CatalogType);

        if (type == null)
        {
            type = new CatalogType
            {
                Type = catalogItemDTO.CatalogType
            };

            _context.CatalogTypes.Add(type);
        }

        var brand =
            await _context.CatalogBrands
                .FirstOrDefaultAsync(
                    c => c.Brand == catalogItemDTO.CatalogBrand);

        if (brand == null)
        {
            brand = new CatalogBrand
            {
                Brand = catalogItemDTO.CatalogBrand
            };

            _context.CatalogBrands.Add(brand);
        }

        catalogItem.Name = catalogItemDTO.Name;
        catalogItem.Description = catalogItemDTO.Description;
        catalogItem.Price = catalogItemDTO.Price;
        catalogItem.CatalogType = type;
        catalogItem.CatalogBrand = brand;
        catalogItem.AvailableStock = catalogItemDTO.AvailableStock;
        catalogItem.RestockThreshold = catalogItemDTO.RestockThreshold;
        catalogItem.MaxStockThreshold = catalogItemDTO.MaxStockThreshold;
        catalogItem.OnReorder = catalogItemDTO.OnReorder;

        await _context.SaveChangesAsync();

        return NoContent();
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
