using Bookline.Application.Common.Models;
using Bookline.Application.Customers.Commands;
using Bookline.Application.Customers.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers(
        [FromQuery] string? searchQuery = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(new GetCustomersQuery(searchQuery, includeArchived, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(Guid id)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var result = await _sender.Send(new CreateCustomerCommand(request));
        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await _sender.Send(new UpdateCustomerCommand(id, request));
        return Ok(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult> ArchiveCustomer(Guid id)
    {
        await _sender.Send(new ArchiveCustomerCommand(id));
        return NoContent();
    }
}
