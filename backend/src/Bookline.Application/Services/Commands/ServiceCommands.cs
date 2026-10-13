using Bookline.Application.Services.DTOs;
using MediatR;

namespace Bookline.Application.Services.Commands;

// Service Categories
public record GetServiceCategoriesQuery : IRequest<List<ServiceCategoryDto>>;
public record CreateServiceCategoryCommand(CreateServiceCategoryRequest Request) : IRequest<ServiceCategoryDto>;
public record UpdateServiceCategoryCommand(Guid Id, UpdateServiceCategoryRequest Request) : IRequest<ServiceCategoryDto>;
public record DeleteServiceCategoryCommand(Guid Id) : IRequest<bool>;

// Services
public record GetServicesQuery(Guid? CategoryId = null, bool IncludeArchived = false) : IRequest<List<ServiceDto>>;
public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto>;
public record GetServiceBySlugQuery(string Slug) : IRequest<ServiceDto>;
public record CreateServiceCommand(CreateServiceRequest Request) : IRequest<ServiceDto>;
public record UpdateServiceCommand(Guid Id, UpdateServiceRequest Request) : IRequest<ServiceDto>;
public record ArchiveServiceCommand(Guid Id) : IRequest<bool>;
public record DuplicateServiceCommand(Guid Id) : IRequest<ServiceDto>;
public record AssignServiceStaffCommand(Guid ServiceId, List<Guid> StaffIds) : IRequest<ServiceDto>;
public record AssignServiceLocationsCommand(Guid ServiceId, List<Guid> LocationIds) : IRequest<ServiceDto>;
public record SetServicePublishStatusCommand(Guid ServiceId, bool IsPublished) : IRequest<ServiceDto>;
