using Bookline.Application.Tenants.DTOs;
using MediatR;

namespace Bookline.Application.Tenants.Commands;

public record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;

public record SaveOnboardingStepCommand(SaveOnboardingStepRequest Request) : IRequest<OnboardingStatusDto>;

public record GetOnboardingReadinessQuery : IRequest<OnboardingReadinessDto>;

public record PublishStorefrontCommand : IRequest<PublishStorefrontResultDto>;

public record CompleteOnboardingCommand : IRequest<OnboardingStatusDto>;
