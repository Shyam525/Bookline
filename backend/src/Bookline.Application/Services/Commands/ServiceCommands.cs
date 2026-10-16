using Bookline.Application.Services.DTOs;
using MediatR;

namespace Bookline.Application.Services.Commands;

// Service Categories
public record GetServiceCategoriesQuery : IRequest<List<ServiceCategoryDto>>;
public record CreateServiceCategoryCommand(CreateServiceCategoryRequest Request) : IRequest<ServiceCategoryDto>;
public record UpdateServiceCategoryCommand(Guid Id, UpdateServiceCategoryRequest Request) : IRequest<ServiceCategoryDto>;
public record DeleteServiceCategoryCommand(Guid Id) : IRequest<bool>;

// Services
public record GetServicesQuery(
    Guid? CategoryId = null,
    Guid? LocationId = null,
    Guid? StaffId = null,
    string? Status = null,
    bool IncludeArchived = false,
    string? Search = null
) : IRequest<List<ServiceDto>>;

public record GetPagedServicesQuery(
    Guid? CategoryId = null,
    Guid? LocationId = null,
    Guid? StaffId = null,
    string? Status = null,
    bool IncludeArchived = false,
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? Sort = "name",
    string? Direction = "asc"
) : IRequest<Bookline.Application.Common.Models.PagedResult<ServiceDto>>;

public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto>;
public record GetServiceBySlugQuery(string Slug) : IRequest<ServiceDto>;
public record CreateServiceCommand(CreateServiceRequest Request) : IRequest<ServiceDto>;
public record UpdateServiceCommand(Guid Id, UpdateServiceRequest Request) : IRequest<ServiceDto>;
public record ArchiveServiceCommand(Guid Id) : IRequest<bool>;
public record DuplicateServiceCommand(Guid Id) : IRequest<ServiceDto>;
public record AssignServiceStaffCommand(Guid ServiceId, List<Guid> StaffIds) : IRequest<ServiceDto>;
public record AssignServiceLocationsCommand(Guid ServiceId, List<Guid> LocationIds, List<LocationPriceOverrideDto>? PriceOverrides = null) : IRequest<ServiceDto>;
public record ConfigureServiceResourcesCommand(Guid ServiceId, bool RequiresResource, string? RequiredResourceType = null, Guid? RequiredResourceId = null) : IRequest<ServiceDto>;
public record SetServicePublishStatusCommand(Guid ServiceId, bool IsPublished) : IRequest<ServiceDto>;
public record SetServiceStatusCommand(Guid ServiceId, string Status) : IRequest<ServiceDto>;

// Resources
public record GetResourcesQuery(Guid? LocationId = null, string? Type = null, bool? IsActive = null) : IRequest<List<ResourceDto>>;
public record GetResourceByIdQuery(Guid Id) : IRequest<ResourceDto>;
public record CreateResourceCommand(CreateResourceRequest Request) : IRequest<ResourceDto>;
public record UpdateResourceCommand(Guid Id, UpdateResourceRequest Request) : IRequest<ResourceDto>;
public record DeleteResourceCommand(Guid Id) : IRequest<bool>;

