using Bookline.Application.Common.Models;
using Bookline.Application.Services.Commands;
using Bookline.Application.Staff.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace Bookline.Application.UnitTests;

public class CoreCrudValidationTests
{
    [Fact]
    public void CreateServiceCommandValidator_NegativeDuration_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateServiceCommandValidator();
        var command = new CreateServiceCommand("Haircut", -15, 5, 25.00m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Fact]
    public void CreateServiceCommandValidator_NegativePrice_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateServiceCommandValidator();
        var command = new CreateServiceCommand("Haircut", 30, 5, -10.00m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void SetWorkingHoursCommandValidator_EndTimeBeforeStartTime_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SetWorkingHoursCommandValidator();
        var workingHours = new List<WorkingHourInput>
        {
            new(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(9, 0)) // End 9:00 AM before Start 5:00 PM
        };
        var command = new SetWorkingHoursCommand(Guid.NewGuid(), workingHours);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void SetWorkingHoursCommandValidator_OverlappingWorkingHours_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SetWorkingHoursCommandValidator();
        var workingHours = new List<WorkingHourInput>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            new(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(17, 0)) // Overlaps 12:00-13:00
        };
        var command = new SetWorkingHoursCommand(Guid.NewGuid(), workingHours);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void CreateTimeOffCommandValidator_EndBeforeStart_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateTimeOffCommandValidator();
        var startUtc = DateTimeOffset.UtcNow;
        var endUtc = startUtc.AddHours(-2); // End before start
        var command = new CreateTimeOffCommand(Guid.NewGuid(), startUtc, endUtc, "Vacation");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndUtc);
    }

    [Fact]
    public void PagedQuery_PageSizeExceeding100_ShouldBeCappedAt100()
    {
        // Act
        var query = new PagedQuery(Page: 1, PageSize: 500);

        // Assert
        Assert.Equal(100, query.PageSize);
    }
}
