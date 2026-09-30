using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Bookline.Application.Services.Commands;

public record ServiceDto(
    Guid Id,
    string Name,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price,
    bool IsActive
);

public record CreateServiceCommand(
    string Name,
    int DurationMinutes,
    int BufferMinutes,
    decimal Price
) : IRequest<ServiceDto>;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(100).WithMessage("Service name must not exceed 100 characters.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Service duration must be greater than 0 minutes.");

        RuleFor(x => x.BufferMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Buffer duration cannot be negative.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
    }
}
