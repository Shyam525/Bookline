using Bookline.Application.Services.Commands;
using Bookline.Application.Services.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookline.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
[Route("api/provider/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly ISender _sender;

    public ServicesController(ISender sender)
    {
        _sender = sender;
    }

    // --- Service Categories ---

    /// <summary>
    /// Lists all service categories for the authorized business.
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetCategories()
    {
        var result = await _sender.Send(new GetServiceCategoriesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Creates a new service category for the authorized business.
    /// </summary>
    [HttpPost("categories")]
    public async Task<ActionResult<ServiceCategoryDto>> CreateCategory([FromBody] CreateServiceCategoryRequest request)
    {
        var result = await _sender.Send(new CreateServiceCategoryCommand(request));
        return Ok(result);
    }

    /// <summary>
    /// Updates a service category.
    /// </summary>
    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<ServiceCategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateServiceCategoryRequest request)
    {
        var result = await _sender.Send(new UpdateServiceCategoryCommand(id, request));
        return Ok(result);
    }

    /// <summary>
    /// Deletes a service category if it contains no active services.
    /// </summary>
    [HttpDelete("categories/{id:guid}")]
    public async Task<ActionResult> DeleteCategory(Guid id)
    {
        await _sender.Send(new DeleteServiceCategoryCommand(id));
        return NoContent();
    }

    // --- Services ---

    /// <summary>
    /// Lists services for the authorized business with optional filtering by category, location, staff, status, search.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetServices(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? staffId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] string? search = null)
    {
        var result = await _sender.Send(new GetServicesQuery(categoryId, locationId, staffId, status, includeArchived, search));
        return Ok(result);
    }

    /// <summary>
    /// Section 84: Lists services with bounded pagination, filters, and sort allowlist.
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<Bookline.Application.Common.Models.PagedResult<ServiceDto>>> GetPagedServices(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? staffId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "name",
        [FromQuery] string? direction = "asc")
    {
        var result = await _sender.Send(new GetPagedServicesQuery(categoryId, locationId, staffId, status, includeArchived, search, page, pageSize, sort, direction));
        return Ok(result);
    }

    /// <summary>
    /// Gets service details by unique service ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> GetServiceById(Guid id)
    {
        var result = await _sender.Send(new GetServiceByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Gets service details by public slug.
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ServiceDto>> GetServiceBySlug(string slug)
    {
        var result = await _sender.Send(new GetServiceBySlugQuery(slug));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new service under the authorized business.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceRequest request)
    {
        var result = await _sender.Send(new CreateServiceCommand(request));
        return CreatedAtAction(nameof(GetServiceById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing service under the authorized business.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
    {
        var result = await _sender.Send(new UpdateServiceCommand(id, request));
        return Ok(result);
    }

    /// <summary>
    /// Updates service status (Active, Inactive, Draft, Archived).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ServiceDto>> SetStatus(Guid id, [FromBody] SetServiceStatusRequest request)
    {
        var result = await _sender.Send(new SetServiceStatusCommand(id, request.Status));
        return Ok(result);
    }

    /// <summary>
    /// Assigns eligible staff members to a service.
    /// </summary>
    [HttpPut("{id:guid}/staff")]
    public async Task<ActionResult<ServiceDto>> AssignServiceStaff(Guid id, [FromBody] List<Guid> staffIds)
    {
        var result = await _sender.Send(new AssignServiceStaffCommand(id, staffIds));
        return Ok(result);
    }

    /// <summary>
    /// Configures location applicability and optional price overrides for a service.
    /// </summary>
    [HttpPut("{id:guid}/locations")]
    public async Task<ActionResult<ServiceDto>> AssignServiceLocations(Guid id, [FromBody] List<Guid> locationIds)
    {
        var result = await _sender.Send(new AssignServiceLocationsCommand(id, locationIds));
        return Ok(result);
    }

    /// <summary>
    /// Configures required resources for a service (treatment room, chair, machine).
    /// </summary>
    [HttpPut("{id:guid}/resources")]
    public async Task<ActionResult<ServiceDto>> ConfigureServiceResources(Guid id, [FromBody] ConfigureServiceResourcesRequest request)
    {
        var result = await _sender.Send(new ConfigureServiceResourcesCommand(id, request.RequiresResource, request.RequiredResourceType, request.RequiredResourceId));
        return Ok(result);
    }

    /// <summary>
    /// Sets whether the service is published to the public marketplace storefront.
    /// </summary>
    [HttpPut("{id:guid}/publish")]
    public async Task<ActionResult<ServiceDto>> SetPublishStatus(Guid id, [FromBody] SetPublishStatusRequest request)
    {
        var result = await _sender.Send(new SetServicePublishStatusCommand(id, request.IsPublished));
        return Ok(result);
    }

    /// <summary>
    /// Archives a service, deactivating it and hiding it from standard listings.
    /// </summary>
    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult> ArchiveService(Guid id)
    {
        await _sender.Send(new ArchiveServiceCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Duplicates an existing service with copy suffix.
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ServiceDto>> DuplicateService(Guid id)
    {
        var result = await _sender.Send(new DuplicateServiceCommand(id));
        return Ok(result);
    }
}

public record SetPublishStatusRequest(bool IsPublished);

