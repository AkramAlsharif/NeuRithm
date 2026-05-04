namespace Neurithm.Application.Services;

using Neurithm.Core.Interfaces;
using Neurithm.Core.Models;

/// <summary>
/// Core service for pitch detection and note conversion.
/// Converts frequencies to musical notes using equal temperament.
/// </summary>
public class PitchDetectionService : IPitchDetectionService
{
    private const double SemitonesPerOctave = 12.0;
    private const int A4MidiNumber = 69;

    private readonly double _a4FrequencyHz;

    public PitchDetectionService(double a4FrequencyHz = 440.0)
    {
        _a4FrequencyHz = a4FrequencyHz;
    }

    /// <summary>
    /// Convert frequency to the closest musical note.
    /// Uses equal temperament formula.
    /// </summary>
    public Note FrequencyToNote(double frequencyHz, double volumeAmount, long timestampMs)
    {
        if (frequencyHz <= 0)
        {
            throw new ArgumentException("Frequency must be positive", nameof(frequencyHz));
        }

        // Calculate MIDI note number using equal temperament formula
        // MIDI = 69 + 12 * log2(f / 440)
        var midiNumber = A4MidiNumber + 12 * Math.Log2(frequencyHz / _a4FrequencyHz);
        var midiNumberRounded = (int)Math.Round(midiNumber);

        // Clamp to valid range
        midiNumberRounded = Math.Max(0, Math.Min(127, midiNumberRounded));

        // Get the ideal frequency for this MIDI note
        var idealFrequency = _a4FrequencyHz * Math.Pow(2, (midiNumberRounded - A4MidiNumber) / 12.0);

        // Calculate cents offset (100 cents = 1 semitone)
        var centsOffset = 1200 * Math.Log2(frequencyHz / idealFrequency);

        // Get note name
        var noteName = MidiNumberToNoteName(midiNumberRounded);

        // Calculate confidence based on cents offset (closer to ideal = higher confidence)
        // Full confidence at ±15 cents, 50% confidence at ±50 cents
        var confidence = Math.Max(0, 1.0 - Math.Abs(centsOffset) / 150.0);

        return new Note
        {
            NoteName = noteName,
            MidiNumber = midiNumberRounded,
            Frequency = frequencyHz,
            CentsOffset = centsOffset,
            Confidence = confidence,
            Volume = volumeAmount,
            TimestampMs = timestampMs
        };
    }

    /// <summary>
    /// Convert note name to frequency.
    /// </summary>
    public double NoteToFrequency(string noteName)
    {
        var midiNumber = NoteNameToMidiNumber(noteName);
        return _a4FrequencyHz * Math.Pow(2, (midiNumber - A4MidiNumber) / 12.0);
    }

    /// <summary>
    /// Parse note name like "C4" into note and octave.
    /// </summary>
    public (char Note, int Octave) ParseNoteName(string noteName)
    {
        if (string.IsNullOrEmpty(noteName) || noteName.Length < 2)
        {
            throw new ArgumentException("Invalid note name", nameof(noteName));
        }

        var noteChar = char.ToUpper(noteName[0]);
        if (!int.TryParse(noteName[1..], out var octave))
        {
            throw new ArgumentException("Invalid octave number", nameof(noteName));
        }

        return (noteChar, octave);
    }

    /// <summary>
    /// Get note name from MIDI number.
    /// </summary>
    public string MidiNumberToNoteName(int midiNumber)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var noteName = noteNames[midiNumber % 12];
        var octave = midiNumber / 12 - 1;
        return $"{noteName}{octave}";
    }

    /// <summary>
    /// Get MIDI number from note name.
    /// </summary>
    public int NoteNameToMidiNumber(string noteName)
    {
        var (note, octave) = ParseNoteName(noteName);

        var noteOffsets = new Dictionary<char, int>
        {
            { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 },
            { 'G', 7 }, { 'A', 9 }, { 'B', 11 }
        };

        if (!noteOffsets.TryGetValue(note, out var baseOffset))
        {
            throw new ArgumentException($"Invalid note: {note}", nameof(noteName));
        }

        // Handle accidentals
        var offset = baseOffset;
        if (noteName.Length > 2)
        {
            var accidental = noteName[1];
            offset = accidental == '#' ? baseOffset + 1 : baseOffset;
        }

        return (octave + 1) * 12 + offset;
    }
}
