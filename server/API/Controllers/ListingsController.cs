using Infra;
using Infra.Entities;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/listings")]

public class ListingsController(AppDb db) : ControllerBase
{
    public record ListingRequest(
        Guid VendorId,
        string CategoryId,
        string Title,
        string Description,
        decimal Price,
        int Stock);

    [HttpGet]
    public IActionResult GetAll() => Ok(db.Listings.Where(x => x.IsActive).ToList());

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        var listing = db.Listings.FirstOrDefault(x => x.ListingId == id);
        return listing is null ? NotFound() : Ok(listing);
    }
    
    [HttpGet("vendor/{vendorId:guid}")]
    public IActionResult GetForVendor(Guid vendorId) =>
        Ok(db.Listings.Where(x => x.VendorId == vendorId).ToList());

    [HttpPost]
    public IActionResult Create(ListingRequest request)
    {
        if (request.Price < 0 || request.Stock < 0)
            return BadRequest("Price or stock cannot be negative.");
        
        if (!db.Customers.Any(x => x.CustomerId == request.VendorId))
            return BadRequest("Vendor doesn't exist, hmm");

        var listing = new Listing
        {
            VendorId = request.VendorId,
            CategoryId = request.CategoryId.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            IsActive = request.Stock > 0
        };
        
        db.Insert(listing);
        return CreatedAtAction(nameof(Get), new { id = listing.ListingId }, listing);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, ListingRequest request)
    {
        var listing = db.Listings.FirstOrDefault(x => x.ListingId == id);
        if (listing is null) return NotFound();
        if (listing.VendorId != request.VendorId)
            return BadRequest("Only the owner can update this listing, DUH");
        if (request.Price < 0 || request.Stock < 0)
            return BadRequest("Price or stock cannot be negative");
        
        listing.CategoryId = request.CategoryId.Trim();
        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.Price = request.Price;
        listing.Stock = request.Stock;
        listing.IsActive = request.Stock > 0;
        
        db.Update(listing);
        return Ok(listing);
    }
    
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id, [FromQuery] Guid vendorId)
    {
        var listing = db.Listings.FirstOrDefault(x => x.ListingId == id);
        if (listing is null) return NotFound();
        if (listing.VendorId != vendorId) return Forbid();

        // only soft-deleting so old purchases still point to the listing n all
        listing.IsActive = false;
        listing.Stock = 0;
        db.Update(listing);
        return NoContent();
    }
}