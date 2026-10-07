using Infra;
using Infra.Entities;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/purchases")]
public class PurchasesController (AppDb db) : ControllerBase
{
   public record PurchaseRequest(Guid BuyerId, Guid ListingId, int Quantity);

    [HttpGet ("buyer/{buyerId:guid}")]
    public IActionResult GetForBuyer(Guid buyerId) =>
        Ok(db.Purchases.Where(x => x.BuyerId == buyerId).OrderByDescending(x => x.PurchasedAt).ToList());
        
    [HttpGet("vendor/{vendorId:guid}")]
    public IActionResult GetForVendor(Guid vendorId) => 
        Ok(db.Purchases.Where(x => x.VendorId == vendorId).OrderByDescending(x => x.PurchasedAt).ToList());

    [HttpPost]
    public IActionResult Purchase(PurchaseRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Please actually buy something lol");
        
        var buyer = db.Customers.FirstOrDefault(x => x.CustomerId == request.BuyerId);
        var listing = db.Listings.FirstOrDefault(x => x.ListingId == request.ListingId);
        
        if (buyer is null || listing is null) return NotFound();
        if (!listing.IsActive) return BadRequest("Sorry, the listing is not active anymore");
        if (listing.VendorId == buyer.CustomerId) return BadRequest("You can't buy your own listing :( " +
                                                                       "No market manipulation allowed");
        if (listing.Stock < request.Quantity) return BadRequest("The seller ain't got enough of this item, sorry");
        
        var total = listing.Price * request.Quantity;
        if (buyer.Balance < total) return BadRequest("Broke ass bitch, you don't have enough money in your wallet");
        
        var vendor = db.Customers.FirstOrDefault(x => x.CustomerId == listing.VendorId);
        if (vendor is null) return BadRequest("The seller doesn't exist anymore, another one bites the dust");
        
        using var tx = db.BeginTransaction();
        
        buyer.Balance -= total;
        vendor.Balance += total;
        listing.Stock -= request.Quantity;
        listing.IsActive = listing.Stock > 0;
        
        db.Update(buyer);
        db.Update(vendor);
        db.Update(listing);
        
        var purchase = new Purchase
        {
            BuyerId = buyer.CustomerId,
            VendorId = vendor.CustomerId,
            ListingId = listing.ListingId,
            Quantity = request.Quantity,
            UnitPrice = listing.Price,
            TotalPrice = total
        };
        
        db.Insert(purchase);
        db.Insert(new WalletTransaction
        {
            CustomerId = buyer.CustomerId,
            Amount = -total,
            Type = "purchase",
            Description = $"Purchased {request.Quantity} of {listing.Title} from {vendor.Username}"
        });
        
        db.Insert(new WalletTransaction
        {
            CustomerId = vendor.CustomerId,
            Amount = total,
            Type = "Sale",
            Description = $"Sale {purchase.PurchaseId}"
        });
        
        tx.Commit();
        return Ok(purchase);
    }

}