namespace Bookline.Application.Common.Models;

public class AppConfig
{
    public const string SectionName = "AppConfig";

    public string LaunchCity { get; set; } = "Ahmedabad";
    public string DefaultTimezone { get; set; } = "Asia/Kolkata";
    public string DefaultState { get; set; } = "Gujarat";
    public string DefaultCountry { get; set; } = "India";
}
