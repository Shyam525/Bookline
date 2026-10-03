using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly ISender _sender;

    public ServicesController(ISender sender)
    {
        _sender = sender;
    }

    // --- Service Categories ---

    [HttpGet("categories")]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetCategories()
    {
        var result = await _sender.Send(new GetServiceCategoriesQuery());
        return Ok(result);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ServiceCategoryDto>> CreateCategory([FromBody] CreateServiceCategoryRequest request)
    {
        var result = await _sender.Send(new CreateServiceCategoryCommand(request));
        return Ok(result);
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<ServiceCategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateServiceCategoryRequest request)
    {
        var result = await _sender.Send(new UpdateServiceCategoryCommand(id, request));
        return Ok(result);
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<ActionResult> DeleteCategory(Guid id)
    {
        await _sender.Send(new DeleteServiceCategoryCommand(id));
        return NoContent();
    }

    // --- Services ---

    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetServices(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool includeArchived = false)
    {
        var result = await _sender.Send(new GetServicesQuery(categoryId, includeArchived));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> GetServiceById(Guid id)
    {
        var result = await _sender.Send(new GetServiceByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceRequest request)
    {
        var result = await _sender.Send(new CreateServiceCommand(request));
        return CreatedAtAction(nameof(GetServiceById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
    {
        var result = await _sender.Send(new UpdateServiceCommand(id, request));
        return Ok(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult> ArchiveService(Guid id)
    {
        await _sender.Send(new ArchiveServiceCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ServiceDto>> DuplicateService(Guid id)
    {
        var result = await _sender.Send(new DuplicateServiceCommand(id));
        return Ok(result);
    }
}
