using Bookline.Application.Availability.DTOs;
using Bookline.Application.Staff.Commands;
using MediatR;

namespace Bookline.Application.Availability.Commands;

public record GetAvailabilitySlotsQuery(
    Guid ServiceId,
    Guid? StaffId,
    string Date, // YYYY-MM-DD
    string Timezone = "UTC"
) : IRequest<List<TimeSlotDto>>;

public record GetStaffTimeOffQuery(Guid StaffId) : IRequest<List<TimeOffDto>>;

public record DeleteTimeOffCommand(Guid StaffId, Guid TimeOffId) : IRequest<bool>;
