using Bookline.Application.Tenants.Commands;
using Bookline.Application.Tenants.DTOs;
using Bookline.Application.Tenants.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Bookline.Application.UnitTests;

public class OnboardingValidationTests
{
    private readonly SaveOnboardingStepCommandValidator _validator = new();

    [Fact]
    public void Validator_ValidRequest_PassesValidation()
    {
        var request = new SaveOnboardingStepRequest(
            Step: 3,
            LocationName: "Downtown Branch",
            Address: "100 Main St",
            City: "New York",
            TimeZoneId: "America/New_York",
            Currency: "USD",
            HoldDurationMinutes: 10,
            MinimumNoticeHours: 2,
            BookingHorizonDays: 60,
            CancellationWindowHours: 24,
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0)
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_InvalidIanaTimeZone_FailsValidation()
    {
        var request = new SaveOnboardingStepRequest(
            Step: 3,
            TimeZoneId: "Mars/Crater"
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.TimeZoneId)
            .WithErrorMessage("TimeZoneId must be a valid IANA timezone identifier (e.g., 'America/New_York', 'Asia/Kolkata', 'UTC').");
    }

    [Fact]
    public void Validator_UnsupportedCurrency_FailsValidation()
    {
        var request = new SaveOnboardingStepRequest(
            Step: 3,
            Currency: "CRYPTO_COIN"
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Currency)
            .WithErrorMessage("Currency must be a supported 3-letter currency code (e.g., USD, EUR, GBP, INR, AED).");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(61)]
    public void Validator_HoldDurationOutOfRange_FailsValidation(int invalidDuration)
    {
        var request = new SaveOnboardingStepRequest(
            Step: 3,
            HoldDurationMinutes: invalidDuration
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.HoldDurationMinutes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(169)]
    public void Validator_MinimumNoticeHoursOutOfRange_FailsValidation(int invalidNotice)
    {
        var request = new SaveOnboardingStepRequest(
            Step: 3,
            MinimumNoticeHours: invalidNotice
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.MinimumNoticeHours);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validator_BookingHorizonDaysOutOfRange_FailsValidation(int invalidHorizon)
    {
        var request = new SaveOnboardingStepRequest(
            Step: 3,
            BookingHorizonDays: invalidHorizon
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.BookingHorizonDays);
    }

    [Fact]
    public void Validator_EndTimeBeforeStartTime_FailsValidation()
    {
        var request = new SaveOnboardingStepRequest(
            Step: 5,
            StartTime: new TimeOnly(18, 0),
            EndTime: new TimeOnly(9, 0)
        );

        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.EndTime)
            .WithErrorMessage("Operating hours EndTime must be after StartTime.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(-1)]
    public void Validator_StepOutOfRange_FailsValidation(int invalidStep)
    {
        var request = new SaveOnboardingStepRequest(Step: invalidStep);
        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Step);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public void Validator_ValidSteps_PassesStepValidation(int validStep)
    {
        var request = new SaveOnboardingStepRequest(Step: validStep);
        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Request.Step);
    }

    [Fact]
    public void Validator_InvalidServiceDurationOrPrice_FailsValidation()
    {
        var request = new SaveOnboardingStepRequest(
            Step: 7,
            FirstServiceDurationMinutes: 0,
            FirstServicePrice: -10m
        );
        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.FirstServiceDurationMinutes);
        result.ShouldHaveValidationErrorFor(x => x.Request.FirstServicePrice);
    }

    [Fact]
    public void Validator_InvalidProductPriceOrStock_FailsValidation()
    {
        var request = new SaveOnboardingStepRequest(
            Step: 8,
            FirstProductPrice: -5m,
            FirstProductStock: -1
        );
        var command = new SaveOnboardingStepCommand(request);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.FirstProductPrice);
        result.ShouldHaveValidationErrorFor(x => x.Request.FirstProductStock);
    }
}
