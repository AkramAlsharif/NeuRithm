namespace Neurithm.Web.Models;

public sealed record AvatarSummary(
    string Id,
    string Name,
    string Gender,
    string Description,
    string ImagePath,
    bool IsUnlocked,
    string? ThemeColor);
