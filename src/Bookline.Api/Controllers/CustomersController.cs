using Bookline.Application.Common.Models;
using Bookline.Application.Customers.Commands;
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

    [HttpPost]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "ManageBookings")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new GetCustomersQuery(page, pageSize));
        return Ok(result);
    }
}
