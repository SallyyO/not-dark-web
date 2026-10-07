using Infra;
using Infra.Entities;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/customers")]

public class CustomersController(AppDb db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(db.Customers.ToList());

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var customer = db.Customers.FirstOrDefault(x => x.CustomerId == id);
        return customer is null ? NotFound() : Ok(customer);
    }
    
    public record CreateCustomerRequest(string Username, string Email);
    public record UpdateCustomerRequest(string Username, string Email);
    
    public record DepositRequest(decimal Amount);

    [HttpPost]
    public IActionResult Create(CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Username and email are required.");

        var customer = new Customer
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            Balance = 0m
        };

        db.Insert(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }
    
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, UpdateCustomerRequest request)
    {
        var customer = db.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (customer is null) return NotFound();
        
        customer.Username = request.Username.Trim();
        customer.Email = request.Email.Trim();

        db.Update(customer);
        return Ok(customer);
    }
    
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var customer = db.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (customer is null) return NotFound();

        db.Delete(customer);
        return Ok(customer);
    }

    [HttpGet("{id:guid}/wallet")]
    public IActionResult GetWallet(Guid id)
    {
        var customer = db.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (customer is null) return NotFound();

       var transactions = db.WalletTransactions
            .Where(x => x.CustomerId == id)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
            
        return Ok(new { customer.CustomerId, customer.Balance, transactions });
        
    }
    
    
    // Lil wallet top-up to test it
    [HttpPost("{id:guid}/wallet/deposit")]
    public IActionResult Deposit(Guid id, DepositRequest request)
    {
        if (request.Amount <= 0) return BadRequest("Deposit amount must be greater than zero.");

        var customer = db.Customers.FirstOrDefault(x => x.CustomerId == id);
        if (customer is null) return NotFound();

        using var tx = db.BeginTransaction();
        customer.Balance += request.Amount;
        db.Update(customer);

        db.Insert(new WalletTransaction
        {
            CustomerId = id,
            Amount = request.Amount,
            Type = "Deposit",
            Description = "Wallet top-up"
        });

        tx.Commit();
        return Ok(new { customer.CustomerId, customer.Balance });
    }
}