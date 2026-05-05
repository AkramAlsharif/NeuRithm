# GitHub Copilot

GitHub Copilot is an AI-powered code completion tool that helps you write code faster and with fewer errors. It suggests whole lines or blocks of code as you type, based on the context provided by your project and coding patterns.

---

# Project Foundation / MVP Instructions

## Build Status
- [x] Solution and project structure verified
- [x] Core domain models and repository interfaces scaffolded in Neurithm.Core
- [x] Application services scaffolded in Neurithm.Application
- [x] Infrastructure file-based repositories scaffolded in Neurithm.Infrastructure
- [x] Audio abstraction and pitch detection scaffolded in Neurithm.Audio
- [x] Blazor UI pages scaffolded in Neurithm.Web (Home, Profile, LevelSelector, Calibration, Game)
- [x] Sample levels and avatars added to App_Data
- [x] IIS publish and deployment documentation added (PUBLISH_IIS.md)
- [x] IIS publish folder set to D:\web\NeuRithm.net for all deployments
- [x] IIS runtime loading fix applied (publish to root + static file mappings + root default document)
- [x] Browser microphone permission request implemented on Home page via JS interop service
- [x] Deterministic IIS publish script added with deployment logs and health checks
- [x] Publish script corrected to deploy the active solution project path reliably
- [x] WebAssembly startup hardened using InvariantGlobalization to remove ICU .dat dependency
- [x] Dynamic page navigation and local client runtime logs implemented for action diagnostics
- [x] Dynamic level catalog service and Level Selector integration implemented
- [x] IIS deployment script enhanced to append latest IIS error log tail after each deployment
- [x] Dynamic avatar catalog service and Profile page avatar selection implemented
- [x] Legacy template route 404 mitigation and gameplay component foundation added
- [x] Router NotFound configuration conflict fixed and dynamic PianoKeyboard component added

_This status will be updated after each major step._

---

## IIS Publish Folder
- The official IIS publish folder for this project is always:
  ```
  D:\web\NeuRithm.net
  ```
- Always deploy using the script:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\Publish-IIS.ps1
  ```
- The script ensures:
  - fresh publish output from `Neurithm.Web/Neurithm.Web.csproj`
  - static assets copied to IIS root
  - correct web.config with MIME mappings and no-store cache headers
  - IIS site restart
  - health checks for `/` and `/js/microphone.js`
  - local deployment logs
  - latest IIS log inspection for recent 404/500 entries

---

## Project Name
**Neurithm**  
Domain: neurithm.net

## Product Identity
Neurithm is a rhythm-learning piano game where the player uses a real physical piano. The website listens through the microphone, detects the note being played, and uses that input for gameplay.

The game should feel like a mix of:
- Synthesia-style falling piano notes
- Guitar Hero-style timing lanes
- Adaptive music training
- Beginner-friendly classical piano practice

## Technology Requirements
- Use .NET 10
- Use the Blazor Web App template
- Use C# as the main language
- Website only (no desktop or mobile app)
- No React, Vue, Angular, or Node.js frontend
- No JavaScript for game logic, scoring, timing, level loading, UI state, or business rules
- JavaScript only as a thin interop layer for browser microphone APIs if required
- Three.js allowed only for advanced visuals if needed later
- All important logic must remain in C#
- Use clean architecture
- Keep the codebase dynamic and expandable
- All data must be file-based for now
- Storage must be behind interfaces for future database migration

## Project/Solution Structure
- `/src/Neurithm.Web`: Blazor Web App, UI only, DI setup, IIS-ready
- `/src/Neurithm.Core`: Domain models, interfaces, no Blazor/file system/browser API dependencies
- `/src/Neurithm.Application`: Game engine services, DTOs, use cases
- `/src/Neurithm.Infrastructure`: File-based repositories, JSON storage, validation, replaceable by EF Core/database
- `/src/Neurithm.Audio`: Microphone abstraction, pitch detection, frequency-to-note conversion, browser audio interop wrapper if needed
- `/tests/Neurithm.Tests`: Unit tests for core gameplay logic

## Branding
- App name: Neurithm
- Domain: neurithm.net
- Use this name in browser title, header/navbar, home page, profile page, README, and publish/deployment docs
- No copyrighted anime/game/music characters
- Use original anime-inspired characters only

## Data Storage
- Use file-based storage
- Folders: `/App_Data/Levels`, `/App_Data/Scores`, `/App_Data/Settings`, `/App_Data/Profiles`, `/App_Data/Avatars`, `/App_Data/GameHistory`
- Repository interfaces and file implementations for all data
- Game logic and Blazor components must never read files or JSON directly; all access through interfaces

## Home Page Requirements
- Hero section with title, subtitle, and main buttons
- How it works section
- Game modes preview
- Quick stats preview
- System status panel

## Profile Page Requirements
- Create/select local player profile
- Profile fields: display name, avatar, difficulty, A4 tuning, mic sensitivity, latency, last played level, stats
- Avatar selection: 6 original anime-inspired characters (3 female, 3 male)
- Avatar metadata as JSON, images in `/wwwroot/assets/avatars`, placeholders allowed

## Core Gameplay
- Notes fall from top, hit line near bottom
- Player must play matching note on real piano
- Screen piano is visual feedback only
- Microphone detects real piano note
- Scoring based on timing window
- Adaptive tempo: misses slow down, good play restores speed

## Required Pages & Components
- Pages: Home, Game, LevelSelector, Calibration, Profile, FreePlay, Results, Settings
- Components: PianoKeyboard, FallingNotes, HitLine, ScorePanel, ComboIndicator, AccuracyMeter, TempoMeter, LevelCard, AvatarSelector, CalibrationPanel, DebugAudioPanel, CountdownOverlay, PauseMenu, ResultSummary, MicPermissionStatus

## Game Modes
- Calibration, Free Play, Practice, Level Mode (see detailed requirements above)

## Level System
- Levels as JSON in `/App_Data/Levels`
- Dynamic, data-driven, validated before use
- At least two sample public-domain classical-style levels

## Microphone & Pitch Detection
- Request mic permission, detect pitch/frequency, convert to note (A4=440Hz default, configurable)
- Monophonic detection for MVP
- Debug data and calibration options
- JS interop only for mic access if needed

## Visual Piano
- Render on-screen keyboard in Blazor, highlight detected notes, C2–C7 range

## Falling Notes & Timing
- Notes fall visually, hit windows for scoring, feedback for hit quality
- Timing based on BPM, tempo multiplier, configurable hit windows

## Scoring & Adaptive Tempo
- Scoring: Perfect (+3), Good (+2), Acceptable (+1), Miss (-1)
- Track score, combo, accuracy, misses, tempo, progress
- Adaptive tempo based on recent accuracy

## Playability Improvements
- Countdown, pause/resume, metronome, latency calibration, note labels, miss protection, song preview, result screen, progress/history, accessibility options

## Calibration
- Calibration page/panel for mic sensitivity, noise gate, confidence, latency, A4 tuning

## Game Flow
- See detailed steps above

## IIS Publishing Requirements
- Add publish documentation in README
- Support `dotnet publish -c Release -o ./publish`
- Include web.config, run behind IIS, document .NET Hosting Bundle, IIS site setup, appsettings

## Local Domain Setup
- Document hosts file setup for neurithm.net
- Explain HTTPS requirement for mic access

## Configuration
- Use strongly typed options for all settings

## Best Practices
- Dependency injection, interfaces, nullable reference types, thin Razor components, business logic out of components, validation, logging, error handling, no magic numbers, options for config, useful comments, expandable architecture

## Testing
- Unit tests for all core logic and services

## README Requirements
- Explain what Neurithm is, how to run locally, publish to IIS, bind neurithm.net, configure IIS, calibrate mic, add levels, file storage, migration, MVP limitations

## Important Constraints
- No MIDI input, no screen piano for scoring, no hardcoded levels, no JS game logic, no React/Vue/Angular/Node.js, no database yet, no mixing file storage with game logic, no copyrighted anime, prioritize stable gameplay

## Acceptance Criteria
- See detailed list above for MVP completion

---

# Next Steps: Advanced Task List for Neurithm

1. Implement dynamic Blazor components:
   - PianoKeyboard.razor (visual piano, highlight detected notes)
   - FallingNotes.razor (render falling notes, timing feedback)
   - HitLine.razor (target line for note hits)
   - ScorePanel.razor, ComboIndicator.razor, AccuracyMeter.razor, TempoMeter.razor
2. Integrate file-based repositories with Blazor UI:
   - Load levels and avatars dynamically from App_Data
   - Display and select avatars in Profile page
   - Level selection and validation in LevelSelector
3. Implement microphone and pitch detection integration:
   - Request browser mic permission (JS interop)
   - Capture audio frames and process in C#
   - Highlight detected note on PianoKeyboard
4. Implement core gameplay logic:
   - Falling notes, hit detection, scoring, adaptive tempo
   - Result screen and attempt history
5. Add calibration and settings UI:
   - CalibrationPanel.razor (mic sensitivity, latency, A4 tuning)
   - Save/load settings via repository
6. Add file-based profile management:
   - Create/select profile, save stats, avatar, preferences
7. Add unit tests for:
   - Pitch detection
   - Hit detection
   - Level validation
   - Profile and score persistence
8. Polish UI/UX:
   - Add placeholder or real avatar images
   - Responsive layout
   - Accessibility options
9. Update README and documentation as features are completed.

---

*This project uses GitHub Copilot to enhance productivity and code quality.*