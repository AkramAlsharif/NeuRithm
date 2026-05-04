# Neurithm - Learn Piano Rhythm with Your Real Instrument

A rhythm-learning piano game built with .NET 10 and Blazor Web App. Neurithm uses real microphone input to detect notes you play on your actual piano and provides real-time feedback with an adaptive difficulty system.

**Domain:** neurithm.net

## Features

- **Real Piano Input:** Uses microphone to detect notes from a real physical piano
- **Adaptive Difficulty:** Tempo adjusts based on your performance
- **Data-Driven Levels:** Levels loaded from JSON for easy expansion
- **User Profiles:** Create and manage multiple player profiles with progress tracking
- **Avatar System:** 6 original anime-inspired characters
- **Calibration Mode:** Fine-tune microphone sensitivity and frequency detection
- **File-Based Storage:** All data stored as JSON (easily migratable to database)
- **Clean Architecture:** Separation of concerns with Core, Application, Infrastructure, and Audio layers

## Technology Stack

- **.NET:** 10.0
- **Frontend:** Blazor Web App (C# only, no JavaScript for game logic)
- **Architecture:** Clean Architecture with Dependency Injection
- **Storage:** File-based JSON (interfaces for future database migration)
- **Audio:** Microphone input with autocorrelation pitch detection

## Project Structure

```
/src
  /Neurithm.Core                 # Domain models & interfaces
    /Models                      # Note, GameLevel, UserProfile, Avatar, etc.
    /Interfaces                  # Repository & service contracts
  /Neurithm.Application         # Business logic & services
    /DTOs                       # Data transfer objects
    /Services                   # GameEngine, PitchDetection, etc.
  /Neurithm.Infrastructure      # Data access layer
    /Repositories               # File-based implementations
  /Neurithm.Audio               # Audio processing
    # Pitch detection, autocorrelation algorithms
/Components                      # Blazor pages & components
  /Pages                         # Home, Profile, LevelSelector, Game, etc.
  /Layout                        # MainLayout with navbar
/App_Data                        # File-based storage
  /Levels                        # Level JSON files
  /Profiles                      # User profile JSON files
  /Avatars                       # Avatar metadata
  /GameHistory                   # Game result JSON files
  /Settings                      # App settings
/tests
  /Neurithm.Tests               # xUnit tests

```

## Getting Started

### Prerequisites

- .NET 10 SDK
- A modern web browser (Chrome, Firefox, Edge, Safari)
- A microphone and real piano/keyboard for playing

### Local Development

1. **Clone the repository:**
   ```bash
   cd D:\web\NeuRithm\
   ```

2. **Restore packages:**
   ```bash
   dotnet restore
   ```

3. **Run the app:**
   ```bash
   dotnet run
   ```

4. **Open in browser:**
   - Development: `https://localhost:5001` or `http://localhost:5000`
   - Or set up local domain (see Local Domain Setup section)

### Microphone Permissions

**Important:** Microphone access may require HTTPS in production browsers. For local development:
- Use `http://localhost:5000` with development server
- Or configure HTTPS locally (see Local Domain Setup)

## Usage

### Home Page
- View quick stats and system status
- Access main features: Start Playing, Calibrate Microphone, Profile

### Profile Management
- Create new profiles with a display name
- Select from 6 anime-inspired avatars
- Adjust audio calibration settings:
  - A4 tuning frequency (default 440 Hz)
  - Microphone sensitivity
  - Latency offset

### Calibration Mode
- Test microphone input
- View real-time pitch detection data
- See detected note, frequency, confidence, and volume

### Level Selection
- Browse available levels
- Filter by difficulty
- View level details (BPM, note count, duration, preview)

### Gameplay
- Select a level to play
- Hit notes at the right time as they fall to the hit line
- Scores are calculated based on timing accuracy
- Missed notes slow down tempo; good performance speeds it up
- Results are saved to profile history

### Settings
- Accessibility options (reduce animations, larger text, high contrast)
- Audio settings (noise gate, confidence threshold)
- Game preferences (metronome, auto-advance, default difficulty)

## File-Based Storage

All data is stored as JSON files in `App_Data/`:

### Levels (`/Levels/*.json`)
```json
{
  "schemaVersion": 1,
  "id": "beethoven_ode_to_joy_easy",
  "title": "Ode to Joy - Easy",
  "composer": "Beethoven",
  "difficulty": "Beginner",
  "bpm": 90,
  "timeSignature": "4/4",
  "normalTempoMultiplier": 1.0,
  "previewText": "...",
  "notes": [
    { "note": "E4", "beat": 0, "durationBeats": 1 },
    ...
  ]
}
```

### Profiles (`/Profiles/{profileId}.json`)
```json
{
  "id": "uuid",
  "displayName": "Player Name",
  "selectedAvatarId": "female_aria",
  "a4TuningHz": 440.0,
  "microphoneSensitivity": 0.7,
  "latencyOffsetMs": 0,
  "totalScore": 1250,
  "bestCombo": 42,
  "averageAccuracy": 85.5,
  "totalCompletedLevels": 3,
  "createdUtc": "2024-01-15T10:30:00Z",
  "modifiedUtc": "2024-01-15T11:45:00Z"
}
```

### Game Results (`/GameHistory/{resultId}.json`)
```json
{
  "id": "uuid",
  "profileId": "...",
  "levelId": "beethoven_ode_to_joy_easy",
  "finalScore": 245,
  "accuracyPercent": 82.5,
  "maxCombo": 15,
  "perfectCount": 10,
  "goodCount": 3,
  "acceptableCount": 1,
  "missCount": 1,
  "gameMode": "Level",
  "completedUtc": "2024-01-15T11:50:00Z",
  "durationSeconds": 45.0,
  "finalTempoMultiplier": 0.95,
  "hitResults": [...]
}
```

### Settings (`/Settings/*.json`)
- `audio.json` - Pitch detection settings
- `timing.json` - Hit window timing
- `adaptiveTempo.json` - Tempo adjustment rules
- `scoring.json` - Points configuration

## Adding New Levels

1. Create a JSON file in `App_Data/Levels/` with the format shown above
2. File name should be `{level-id}.json` matching the `"id"` field
3. Restart the app or manually refresh the level cache
4. The level will appear in Level Selector

Example: `App_Data/Levels/my_custom_level.json`

```json
{
  "schemaVersion": 1,
  "id": "my_custom_level",
  "title": "My Custom Level",
  "composer": "Your Name",
  "difficulty": "Beginner",
  "bpm": 120,
  "timeSignature": "4/4",
  "normalTempoMultiplier": 1.0,
  "previewText": "A custom level I created.",
  "notes": [
    { "note": "C4", "beat": 0, "durationBeats": 1 },
    { "note": "D4", "beat": 1, "durationBeats": 1 },
    { "note": "E4", "beat": 2, "durationBeats": 1 },
    { "note": "F4", "beat": 3, "durationBeats": 1 }
  ]
}
```

## Database Migration Path

To migrate from file-based storage to a database:

1. The `Core` layer defines `ILevelRepository`, `IUserProfileRepository`, etc.
2. Create new implementations in a new `Neurithm.Persistence` layer
3. Register the new repositories in `Program.cs`
4. The rest of the application requires no changes

Example database implementations already supported:
- Entity Framework Core (SQL Server, PostgreSQL, SQLite)
- MongoDB
- Any custom data layer

## Testing

Run unit tests:
```bash
dotnet test tests/Neurithm.Tests/
```

Tests cover:
- Pitch detection and note conversion
- Game engine scoring and hit detection
- Adaptive tempo calculations
- Level validation
- Profile operations

## Local Domain Setup

### Windows Hosts File

To use `neurithm.net` locally:

1. Open `C:\Windows\System32\drivers\etc\hosts` as Administrator
2. Add these lines:
   ```
   127.0.0.1 neurithm.net
   127.0.0.1 www.neurithm.net
   ```
3. Save the file
4. Configure IIS binding for `neurithm.net` (see IIS Publishing section)

### HTTPS for Local Testing

For microphone access with HTTPS:

1. Create a self-signed certificate:
   ```bash
   dotnet dev-certs https --trust
   ```

2. Configure in `Properties/launchSettings.json`:
   ```json
   "https://neurithm.net:443"
   ```

## Publishing to IIS

### Prerequisites

- IIS installed on Windows Server
- .NET 10 Hosting Bundle installed
- neurithm.net domain configured or local hosts entry

### Step 1: Publish the App

```powershell
dotnet publish -c Release -o ./publish
```

This generates:
- `publish/` folder with all binaries
- `web.config` for IIS configuration
- Static assets in `wwwroot/`

### Step 2: Configure IIS

1. **Create Application Pool:**
   - Name: `NeurithmAppPool`
   - .NET CLR version: `No Managed Code`
   - Managed pipeline mode: `Integrated`

2. **Create IIS Site:**
   - Site name: `Neurithm`
   - Physical path: `C:\inetpub\neurithm` (or your publish folder)
   - Binding type: `http` (or `https` if certificate available)
   - Binding hostname: `neurithm.net`
   - Port: `80` (http) or `443` (https)

3. **Set Permissions:**
   - Give IIS app pool identity (IIS APPPOOL\NeurithmAppPool) write access to:
     - `App_Data/` folder
     - Subdirectories (Levels, Profiles, GameHistory, Settings, etc.)

4. **Enable features in IIS:**
   - Application Initialization
   - HTTP Compression

### Step 3: Configuration Files

The published app looks for `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Neurithm": {
    "AppDataPath": "C:\\inetpub\\neurithm\\App_Data",
    "Environment": "Production"
  }
}
```

### Step 4: DNS/HTTPS

- Point `neurithm.net` domain to your IIS server
- Install SSL certificate for HTTPS (required for microphone access in production)
- Update IIS binding to use HTTPS

## Architecture & Design

### Clean Architecture Layers

**Neurithm.Core:**
- Domain models (immutable records)
- Repository interfaces (no implementation)
- Service interfaces
- No external dependencies

**Neurithm.Application:**
- Business logic (GameEngine, AdaptiveTempoService)
- Service implementations
- DTOs for inter-layer communication
- Orchestrates use cases

**Neurithm.Infrastructure:**
- File-based repository implementations
- JSON serialization/deserialization
- Directory management
- Could be replaced with EF Core implementations

**Neurithm.Audio:**
- Pitch detection algorithms
- Microphone abstraction
- Minimal JavaScript interop for browser APIs

**Components (Blazor):**
- UI only - no business logic
- Uses services through dependency injection
- Renders pages and interactive components

### Key Design Patterns

**Repository Pattern:** All data access through interfaces
**Dependency Injection:** Configured in `Program.cs`
**DTOs:** Transfer data between layers
**Records:** Immutable value objects for models
**Options Pattern:** Configuration via `appsettings.json`

## MVP Limitations

- **Monophonic only:** Single note detection (chords not supported)
- **No backing audio:** MIDI backing track not included
- **No MIDI input:** Microphone only for MVP
- **File storage only:** No database in MVP
- **Limited visuals:** Basic UI (not fully polished graphics)
- **Placeholder audio interop:** Full JS interop for microphone needed for production

## Future Enhancements

- Polyphonic pitch detection (detect chords)
- MIDI input support
- Backing audio tracks
- Database migration (EF Core)
- Mobile app (Blazor Native)
- Cloud synchronization
- Multiplayer features
- Advanced visualizations with Three.js
- Audio playback synthesis
- More avatars and themes

## Contributing

This is a personal project. For modifications:

1. Keep the clean architecture structure
2. Add tests for new features
3. Use file-based storage behind interfaces
4. Keep game logic in C# (not JavaScript)
5. Follow existing code style and naming conventions

## License

Private project. For commercial use, contact the author.

## Support

- **Documentation:** See README sections above
- **Troubleshooting:** Check browser console for JavaScript errors
- **Microphone Issues:** Ensure browser has permission, test in Calibration mode

## Contact

**Website:** neurithm.net  
**Domain:** neurithm.net  
**App Name:** Neurithm

---

**Version:** 1.0.0 (MVP)  
**Last Updated:** January 2024  
**Built with:** .NET 10 Blazor Web App
