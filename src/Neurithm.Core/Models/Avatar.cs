namespace Neurithm.Core.Models;

/// <summary>
/// Represents a game avatar/character.
/// </summary>
public record Avatar
{
    /// <summary>
    /// Unique identifier (e.g., "female_aria").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name (e.g., "Aria").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gender label (e.g., "Female", "Male").
    /// </summary>
    public string Gender { get; init; } = string.Empty;

    /// <summary>
    /// Personality or style description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Path to image file (relative to wwwroot).
    /// </summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>
    /// Whether this avatar is unlocked for the user.
    /// </summary>
    public bool IsUnlocked { get; init; } = true;

    /// <summary>
    /// Optional theme color (hex, e.g., "#FF5733").
    /// </summary>
    public string? ThemeColor { get; init; }
}
