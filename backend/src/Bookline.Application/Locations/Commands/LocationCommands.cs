using Bookline.Application.Locations.DTOs;
using MediatR;

namespace Bookline.Application.Locations.Commands;

public record GetLocationsQuery(bool IncludeArchived = false) : IRequest<List<LocationDto>>;

public record GetLocationByIdQuery(Guid Id) : IRequest<LocationDto>;

public record CreateLocationCommand(CreateLocationRequest Request) : IRequest<LocationDto>;

public record UpdateLocationCommand(Guid Id, UpdateLocationRequest Request) : IRequest<LocationDto>;

public record ArchiveLocationCommand(Guid Id) : IRequest<bool>;
