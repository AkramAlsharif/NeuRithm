namespace Neurithm.Web.Models;

public sealed record LevelSummary(
    string Id,
    string Title,
    string Composer,
    string Difficulty,
    int Bpm,
    int NoteCount,
    string PreviewText);
