# Neurithm — Game-First Production Directive

## 1. Purpose

Redirect Neurithm away from generic scaffolding and into a **playable game-first vertical slice**.

The next milestone is not a better home page, not avatars, not profile polish, not documentation polish, and not more placeholder architecture.

The next milestone is:

> A user opens Neurithm, starts the microphone, plays a real piano note, sees the matching visual key light up, starts one level, and scores/misses notes using the real piano microphone input.

---

## 2. Current audit summary

Based on the current code and produced output, the project is not yet playable.

### Current problems to fix immediately

1. **Game page is still a static scaffold**
   - It uses hardcoded notes.
   - It has fake score/combo/accuracy.
   - It has a manual `Mark Next Note` button.
   - Notes are displayed as a list, not falling notes tied to game time.

2. **Microphone pipeline is split and inconsistent**
   - `WebMicrophoneService` performs note detection internally using zero-crossing logic.
   - `Neurithm.Audio` also contains `IPitchDetector` and `SimplePitchDetector`.
   - There must be one official C# pitch path only.

3. **`BrowserMicrophoneService` in `Neurithm.Audio` is a stub**
   - It returns `Blocked` and says browser capture must be provided by web interop.
   - This must not be registered as the production microphone service.

4. **Application layer still has placeholders**
   - `AdaptiveTempoService` is placeholder logic.
   - `CalibrationService` returns fixed values.
   - `HitDetectionService` only checks offset and does not manage pending notes, note matching, hit state, or misses.
   - `AvatarService` and `ProfileService` still throw `NotImplementedException`, but those are not the game priority now.

5. **Missing production game session engine**
   - There is no proper stateful game loop service.
   - There is no C# session state that owns time, falling notes, hits, misses, combo, score, tempo, and result state.

---

## 3. Non-negotiable direction

### Focus only on the game

Freeze these until the playable vertical slice is accepted:

- avatar polish
- profile polish
- home page polish
- extra pages
- deployment polishing beyond what is needed to run the mic
- README expansion
- visual theme polishing
- unit test projects or demo test code

### Production-only rule

Do not create:

- placeholder methods
- fake scoring
- manual gameplay buttons
- demo-only components
- hardcoded gameplay data in Razor
- JavaScript game logic
- browser-only fake pitch detection
- new test projects

Everything implemented now must be production code.

---

## 4. Required project rules

- Use `.NET 10`.
- Use `Blazor Web App`.
- Use C# for all game logic.
- JavaScript is allowed only for browser microphone capture.
- JavaScript must not contain scoring, timing, pitch conversion, game state, falling-note state, tempo logic, or level logic.
- Keep file-based data under `App_Data`.
- Keep IIS publish path:

```text
D:\WEB\Neurithm.net
```

- Keep local hosts entries:

```text
127.0.0.1 neurithm.net
127.0.0.1 www.neurithm.net
```

- Use HTTPS locally for microphone validation where possible.

---

## 5. First acceptance gate

Do not move to other features until all items below work.

### Gate A — Calibration works

The user must be able to:

1. Open `Calibration`.
2. Click `Start Microphone`.
3. Grant browser microphone permission.
4. Play real piano notes.
5. See live values:
   - permission status
   - detected note
   - frequency Hz
   - confidence
   - cents offset
   - RMS volume
   - reliable/unreliable state
6. See the matching visual piano key light up.
7. See low room noise ignored.
8. See sustained notes debounced so they do not spam gameplay hits.

### Gate B — One playable level works

The user must be able to:

1. Open `Game`.
2. Select/load one real level from `App_Data/Levels`.
3. Click `Start`.
4. See countdown: `3`, `2`, `1`, `Start`.
5. See falling notes move toward a hit line.
6. Play the real piano note near the hit line.
7. Score using microphone input only.
8. Miss notes when they pass the acceptable hit window.
9. See score, combo, accuracy, misses, current detected note, and target note update live.
10. Finish the level and see a simple result summary.

---

## 6. Correct microphone architecture

### Correct ownership

Use this structure:

```text
Neurithm.Web
  WebMicrophoneService
  JS interop only for browser capture

Neurithm.Audio
  IPitchDetector
  SimplePitchDetector or improved detector
  DetectedNoteResult
  PitchDetectorOptions

Neurithm.Application
  GameSessionService consumes DetectedNoteResult
```

### Required flow

```text
Browser microphone
  -> thin JS captures PCM float samples
  -> WebMicrophoneService receives frames
  -> IPitchDetector.AnalyzeFrame(samples, sampleRate)
  -> DetectedNoteResult
  -> Calibration UI and GameSessionService
```

### Refactor requirements

1. Remove zero-crossing note detection from `WebMicrophoneService`.
2. Inject `IPitchDetector` into the web microphone implementation.
3. `WebMicrophoneService` must publish `DetectedNoteResult`, not just a string note name.
4. Do not register the stub `BrowserMicrophoneService` from `Neurithm.Audio` as the real mic implementation.
5. The production mic implementation must be in `Neurithm.Web` because it depends on `IJSRuntime`.
6. `Neurithm.Audio` must remain browser-independent.

### Required `DetectedNoteResult`

Use this model as the official game input event:

```csharp
public sealed record DetectedNoteResult(
    string NoteName,
    double FrequencyHz,
    double Confidence,
    double CentsOffset,
    double VolumeRms,
    DateTime DetectedAtUtc,
    bool IsReliable);
```

### Reliability rule

Only `IsReliable == true` detections can trigger gameplay hits.

Unreliable detections can still be displayed in Calibration for debugging.

---

## 7. Thin JavaScript microphone adapter only

JavaScript is allowed only for:

- `navigator.mediaDevices.getUserMedia`
- creating `AudioContext`
- reading PCM samples
- sending sample frames to C#
- stopping tracks and cleaning resources
- returning exact browser error messages

JavaScript is forbidden for:

- note detection
- pitch detection
- frequency-to-note conversion
- scoring
- hit detection
- falling-note movement
- tempo calculation
- level loading
- game state

### Required JS-to-C# payload

```csharp
public sealed class MicrophoneFrameDto
{
    public float[] Samples { get; set; } = [];
    public int SampleRate { get; set; }
}
```

---

## 8. Calibration page implementation

The calibration page is the first real game checkpoint.

### Required UI

Show:

- `Start Microphone`
- `Stop Microphone`
- permission status
- last browser error
- current note
- frequency Hz
- confidence
- cents offset
- RMS volume
- reliable/unreliable indicator
- A4 tuning value
- noise gate value
- confidence threshold
- debounce milliseconds
- visual piano from C2 to C7

### Required behavior

- The piano key lights up for the latest detected note.
- Reliable detections should be visually distinct from unreliable detections.
- If no reliable note is detected, show `No reliable note` instead of fake data.
- Do not score anything in Calibration.

---

## 9. Game session engine

Create a real C# game engine service in `Neurithm.Application`.

### Required service

```csharp
public interface IGameSessionService
{
    GameSessionState State { get; }

    Task LoadLevelAsync(string levelId, CancellationToken cancellationToken = default);
    void StartCountdown();
    void StartAfterCountdown();
    void Pause();
    void Resume();
    void Reset();
    GameTickResult Tick(DateTimeOffset now);
    GameInputResult HandleDetectedNote(DetectedNoteResult detectedNote);
}
```

Use equivalent signatures if needed, but the responsibilities must remain the same.

### Required state model

Create a state model that contains at minimum:

```text
GamePhase
LevelId
LevelTitle
Bpm
TempoMultiplier
StartedAt
PausedAt
ElapsedGameTimeSeconds
Score
Combo
MaxCombo
AccuracyPercentage
MissCount
PerfectCount
GoodCount
AcceptableCount
TotalResolvedNotes
CurrentDetectedNote
CurrentTargetNote
LastFeedback
PendingNotes
ResolvedNotes
VisibleFallingNotes
```

### Required phases

```text
NotLoaded
Ready
Countdown
Playing
Paused
Completed
Error
```

---

## 10. Level loading rule

For the playable vertical slice, load one real level from file.

### Required folder

```text
App_Data\Levels
```

### Required level model

Each level note must have:

```text
NoteName
Beat
DurationBeats
```

### Runtime conversion

The game engine must convert beats to seconds using BPM:

```text
seconds = beat * (60 / bpm)
```

Duration:

```text
durationSeconds = durationBeats * (60 / bpm)
```

Do not compute this inside Razor.

---

## 11. Falling notes implementation

Current `FallingNotes.razor` lists notes. Replace it with a real falling-note renderer.

### Required inputs

`FallingNotes.razor` should receive a list of visual note states from the game session:

```text
NoteId
NoteName
LaneIndex
TopPercent
HeightPercent
IsInsideHitWindow
IsResolved
Feedback
```

### Rendering rule

- Use absolute positioning inside a board.
- The hit line must stay fixed.
- Notes move because `TopPercent` changes from the game session tick.
- Do not calculate hit logic inside the component.
- Do not list notes as static cards.

### Required board behavior

- Notes appear above the screen before their hit time.
- Notes reach the hit line at the target time.
- Notes continue below the hit line until the miss window expires.
- Resolved notes disappear or show short feedback.

---

## 12. Hit detection requirements

Replace placeholder hit detection.

### Current problem

The existing hit detection checks only timing offset. It does not own pending notes, does not find nearest matching note, and does not resolve misses.

### Correct behavior

When a reliable detected note arrives:

1. Ignore it if game is not in `Playing` phase.
2. Find pending notes with the same `NoteName`.
3. Calculate timing offset against each candidate:

```text
offsetMs = detectedGameTimeMs - targetNoteGameTimeMs
```

4. Pick the closest unresolved candidate inside the acceptable window.
5. Resolve it once.
6. Return feedback:
   - Perfect: `abs(offsetMs) <= 50`
   - Good: `abs(offsetMs) <= 100`
   - Acceptable: `abs(offsetMs) <= 150`
   - Early/Late can be derived from the sign of `offsetMs`.
7. If no candidate is inside the window, do not score.

### Miss detection

On each tick:

1. Check unresolved notes.
2. If current game time is greater than:

```text
targetTimeMs + acceptableWindowMs
```

3. Resolve as `Miss`.
4. Apply score `-1`.
5. Reset combo.
6. Trigger adaptive tempo slowdown.

---

## 13. Scoring rules

Use this scoring exactly for MVP:

```text
Perfect:    +3
Good:       +2
Acceptable: +1
Miss:       -1
```

### Combo

- Increment combo on Perfect, Good, Acceptable.
- Reset combo on Miss.
- Track max combo.

### Accuracy

Calculate from resolved notes only.

Suggested formula:

```text
accuracy = successfulHits / totalResolvedNotes * 100
```

Where successful hits are Perfect + Good + Acceptable.

---

## 14. Adaptive tempo requirements

Replace placeholder adaptive tempo logic with stateful gradual tempo behavior.

### Rules

```text
NormalTempoMultiplier = 1.0
MinimumTempoMultiplier = 0.5
MissPenaltyStep = 0.05
RecoveryStep = 0.01 to 0.02 per resolved good note/tick window
RecentWindowSize = 10 notes
RecoveryThreshold = 80% recent accuracy
```

### Behavior

- On miss: reduce tempo gradually, not below 0.5.
- On good recent performance: recover gradually, not above 1.0.
- Do not jump from 0.5 to 1.0 instantly.
- Tempo affects game time progression and falling note movement.

### Important

Do not recompute tempo directly from accuracy every tick as a static formula.

Maintain tempo as state.

---

## 15. Game clock and tempo

The game must have two concepts:

```text
Real elapsed time
Game elapsed time
```

Tempo multiplier affects game elapsed time.

Example:

```text
if 1 real second passes and tempo is 0.75,
only 0.75 seconds should advance in the game timeline.
```

This ensures falling notes slow down when the player misses.

---

## 16. Game page refactor

Replace the current `Game.razor` behavior.

### Remove

- hardcoded note list
- fake score/combo/accuracy
- `Mark Next Note` button
- gameplay state stored directly in Razor fields
- scoring from button clicks
- note progression by manual click

### Add

- level loading through `ILevelService`
- microphone start/stop controls
- permission status panel
- countdown overlay
- real game session state
- real falling notes
- current target note panel
- current detected note panel
- score panel from game session state
- keyboard highlight from latest detected note
- clear feedback labels: Perfect, Good, Acceptable, Miss, Early, Late

---

## 17. UI components needed for the vertical slice

Implement only these now:

```text
Calibration.razor
Game.razor
PianoKeyboard.razor
FallingNotes.razor
HitLine.razor
ScorePanel.razor
CountdownOverlay.razor
TimingFeedback.razor
MicrophoneStatusPanel.razor
```

Do not work on profile/avatar polish until this list is working.

---

## 18. Piano keyboard requirements

Improve `PianoKeyboard.razor` only enough for gameplay.

Required:

- C2 to C7 range.
- White and black key visual distinction if possible.
- Highlight latest detected note.
- Highlight current target note differently if useful.
- Do not allow screen clicks to score.
- Optional click debug mode must be disabled by default and visually marked as developer-only if present.

---

## 19. Level-first playable slice

Create one short level for validation.

Example:

```json
{
  "schemaVersion": 1,
  "id": "neurithm_first_steps_c_major",
  "title": "First Steps - C Major",
  "composer": "Neurithm",
  "difficulty": "Beginner",
  "bpm": 60,
  "timeSignature": "4/4",
  "normalTempoMultiplier": 1.0,
  "previewText": "A short microphone and timing validation level.",
  "notes": [
    { "note": "C4", "beat": 0, "durationBeats": 1 },
    { "note": "D4", "beat": 1, "durationBeats": 1 },
    { "note": "E4", "beat": 2, "durationBeats": 1 },
    { "note": "F4", "beat": 3, "durationBeats": 1 },
    { "note": "G4", "beat": 4, "durationBeats": 1 },
    { "note": "A4", "beat": 5, "durationBeats": 1 },
    { "note": "B4", "beat": 6, "durationBeats": 1 },
    { "note": "C5", "beat": 7, "durationBeats": 2 }
  ]
}
```

This level is for production validation, not fake data. It is a real local level file.

---

## 20. File/data scope for this milestone

Only these file areas matter now:

```text
App_Data\Levels
App_Data\Settings
App_Data\GameHistory
```

Do not spend time on:

```text
App_Data\Avatars
App_Data\Profiles
```

except to keep the build from breaking.

---

## 21. Game history after level completion

After a level completes, save one attempt record to file.

Required fields:

```text
AttemptId
CreatedAtUtc
LevelId
LevelTitle
Score
AccuracyPercentage
MissCount
PerfectCount
GoodCount
AcceptableCount
MaxCombo
FinalTempoMultiplier
DurationSeconds
```

This should be done only after the playable level works.

---

## 22. IIS/local run requirement

The game must be validated through the intended local website path.

### IIS path

```text
D:\WEB\Neurithm.net
```

### Local domain

```text
https://neurithm.net
https://www.neurithm.net
```

### Hosts file

```text
C:\Windows\System32\drivers\etc\hosts
```

Required entries:

```text
127.0.0.1 neurithm.net
127.0.0.1 www.neurithm.net
```

### Microphone note

Browser microphone access normally requires a secure context. Use HTTPS for IIS local validation.

---

## 23. Required implementation sequence

Follow this exact order.

### Step 1 — Stop the fake game flow

- Remove `Mark Next Note`.
- Remove hardcoded score/combo/accuracy behavior.
- Remove hardcoded note list from `Game.razor`.
- Keep the page compiling.

### Step 2 — Fix microphone service architecture

- Use one official production microphone service registered in DI.
- It must use JS only for PCM capture.
- It must call `IPitchDetector` in C#.
- It must expose `DetectedNoteResult` events.

### Step 3 — Make Calibration real

- Start/stop mic.
- Show detection metrics.
- Highlight piano key.
- Tune noise gate/confidence/debounce until fake room noise is reduced.

### Step 4 — Build game session service

- Create state model.
- Load level.
- Start countdown.
- Tick game time.
- Produce visible falling notes.
- Resolve hits and misses.

### Step 5 — Wire Game page

- Subscribe to microphone detections.
- Send reliable detections to game session.
- Render game state.
- Show falling notes moving.
- Show score and feedback.

### Step 6 — Add adaptive tempo

- Miss slows tempo.
- Good recent hits gradually restore tempo.
- Verify falling notes slow down and recover.

### Step 7 — Save result history

- Save attempt only after completion.

### Step 8 — Publish to IIS

- Publish only after the playable slice works locally.
- Keep path `D:\WEB\Neurithm.net`.

---

## 24. Manual acceptance verification

The next delivery is accepted only when these manual checks pass.

### Calibration checks

```text
[ ] Open Calibration.
[ ] Start microphone.
[ ] Browser permission becomes Granted.
[ ] Play C4; C4 highlights.
[ ] Play D4; D4 highlights.
[ ] Play E4; E4 highlights.
[ ] Frequency, confidence, cents, and RMS update.
[ ] Silence does not trigger reliable fake notes.
[ ] Sustained note does not spam reliable detections too fast.
```

### Game checks

```text
[ ] Open Game.
[ ] Load first C-major level.
[ ] Start countdown.
[ ] Falling notes move toward hit line.
[ ] Play correct real piano note near hit line.
[ ] Score increases.
[ ] Combo increases.
[ ] Wrong/missed notes do not score.
[ ] Missed note subtracts 1.
[ ] Missed note resets combo.
[ ] Missed notes reduce tempo.
[ ] Good play gradually restores tempo.
[ ] Level completes and shows result summary.
```

---

## 25. What to report back

Do not say “foundation complete.”

Report only:

```text
1. Microphone status
   - Works / blocked / exact error

2. Calibration status
   - Which notes were verified
   - Whether confidence/noise gate is stable

3. Game status
   - Level loaded
   - Falling notes moving
   - Real mic scoring working
   - Miss detection working
   - Adaptive tempo working or not

4. Files changed
   - Exact file list

5. How to run
   - Exact local URL
   - Exact IIS publish command if used

6. Remaining blocker
   - Only if something is not working
```

---

## 26. Hard stop conditions

Stop and report the exact blocker if:

- microphone permission is denied or blocked
- HTTPS is not configured and browser blocks mic
- JS interop cannot deliver PCM frames
- `IPitchDetector` receives no samples
- sample rate or buffer format is invalid
- pitch confidence never reaches threshold
- level JSON cannot load
- game timer does not tick reliably

Do not hide these blockers behind UI polish or generic statements.

---

## 27. Final definition of done for this phase

This phase is done only when Neurithm is playable as a small rhythm game:

```text
Real piano -> microphone -> C# pitch detection -> visual key highlight -> falling note hit detection -> score/miss -> adaptive tempo
```

Anything outside that chain is secondary.

## 28. Progress Update (Latest Step)

### Completed in this step

- Updated `PianoKeyboard.razor` to render a full-width C2–C7 keyboard across the game area with dynamic key sizing.
- Kept white/black key distinction and separate highlights for detected note and target note.
- Updated `FallingNotes.razor` to use full-board lane normalization (61 lanes) so falling notes align to the full piano span.
- Kept falling-note position and timing fully data-driven from C# game-session state.
- Confirmed build compiles after these updates.

### Scope compliance

- No fake scoring was added.
- No manual gameplay buttons were added.
- No game logic moved to JavaScript.
- Focus remained on game-first playable vertical-slice behavior.

## 29. Progress Update (HTTPS Microphone Unblock)

### Completed in this step

- Updated `scripts/Publish-IIS.ps1` to enforce local HTTPS readiness for microphone secure-context requirements.
- Added automatic hosts verification for:
  - `127.0.0.1 neurithm.net`
  - `127.0.0.1 www.neurithm.net`
- Added automatic local self-signed certificate creation/reuse for:
  - `neurithm.net`
  - `www.neurithm.net`
- Added IIS HTTPS bindings and certificate mapping for both hostnames.
- Switched deployment health checks to HTTPS URLs.
- Updated microphone JS secure-context error messages to clearly instruct opening:
  - `https://neurithm.net`
  - `https://www.neurithm.net`

### Scope compliance

- Kept JavaScript as thin microphone interop only.
- No gameplay scoring/timing logic moved to JavaScript.
- Continued game-first implementation path and removed deployment ambiguity for microphone access.

## 30. Progress Update (Secure Microphone + Level Runtime Paths)

### Completed in this step

- Fixed local secure-context microphone blocker by automating HTTPS setup in `scripts/Publish-IIS.ps1`:
  - hosts entries validation for `neurithm.net` and `www.neurithm.net`
  - self-signed cert create/reuse
  - HTTPS IIS bindings for both domains
  - HTTPS endpoint health checks
- Updated microphone secure-context error messages in `wwwroot/js/microphone.js` with explicit HTTPS domain guidance.
- Added runtime level files under `wwwroot/data/levels` to fix game-level 404 loading at runtime:
  - `neurithm_first_steps_c_major.json`
  - `beethoven_ode_to_joy_easy.json`

### Scope compliance

- Kept JavaScript limited to microphone interop and error reporting only.
- Kept gameplay logic in C# services.
- Continued game-first vertical slice implementation with real level data loading.

## 31. Progress Update (Game Visual Mode + Levels + Controls)

### Completed in this step

- Added Three.js visual layer from local workspace path `D:\web\NeuRithm\threejs`:
  - copied `three.module.min.js` to `Neurithm.Web/wwwroot/lib/threejs/`
  - added `Neurithm.Web/wwwroot/js/three-visuals.js` for sparkle-rain visual rendering
- Updated game page for requested visual/gameplay behavior:
  - black background gameplay shell
  - purple glowing falling notes
  - top sticky HUD (`ScorePanel`) with score/accuracy and related stats
  - Play/Pause/Resume controls wired to C# game session state
  - level picker wired to dynamic catalog
- Expanded level content (dynamic JSON):
  - fixed `levels-index.json` format
  - added `mozart_twinkle_easy.json`
  - added `neurithm_arpeggio_foundation.json`
  - added `neurithm_rhythm_focus_01.json`
- Improved microphone guidance for secure context trust requirement in browser.

### Scope compliance

- Kept microphone capture JS-only and gameplay logic in C#.
- Kept hit detection/scoring/timing/adaptive tempo in application services.
- Continued game-first implementation from directive with production-oriented dynamic level loading.

## 32. Progress Update (Downward Notes + Clickable Piano Keys)

### Completed in this step

- Updated game session visual mapping so notes move downward on screen over time.
- Upgraded `PianoKeyboard.razor` to be interactive:
  - pointer press activates key state
  - white keys tint slightly gray while pressed
  - state clears on release/leave
- Added note click sound playback via new `wwwroot/js/piano-audio.js`.
- Wired piano audio script in `wwwroot/index.html`.

### Scope compliance

- Falling-note direction remains computed in C# game session logic.
- Piano click is visual/audio interop only.
- Core gameplay scoring/hit logic remains in C# application services.

## 33. Progress Update (Live Piano Sync Visibility + Instant Play)

### Completed in this step

- Added live microphone detection telemetry directly into `MicrophoneStatusPanel`:
  - detected note
  - frequency Hz
  - reliability state
- Wired panel updates in both `Calibration` and `Game` pages so real piano key presses detected by mic are immediately visible.
- Updated game Play flow to start falling notes instantly (no countdown delay) when Play is clicked.

### Scope compliance

- Real note detection/processing remains in C# audio/game services.
- Sync visualization is dynamic and driven from live mic detection events.
- Gameplay start behavior remains controlled by C# game session state.

## 34. Progress Update (Game Mic Sync Area + Corner Mic Monitor)

### Completed in this step

- Removed click-sound keyboard behavior from gameplay flow (screen keyboard remains visual feedback only).
- Tuned default pitch detector thresholds for more responsive real-piano pickup while preserving reliability gating.
- Updated `Game.razor` layout to keep controls/HUD on the black gameplay screen and added:
  - dedicated mic sync test area for real-piano verification
  - top-right corner microphone monitor panel showing detected note/frequency/reliability
- Preserved dynamic game loop and note-highlighting flow from live microphone detections.

### Scope compliance

- Note highlighting is driven by real mic detections, not screen key clicks.
- Gameplay scoring/hit logic remains in C#.
- UI changes remain focused on synchronization visibility and game-first flow.

## 35. Progress Update (Calibration Mic Frame Flow Fix)

### Completed in this step

- Hardened browser microphone capture in `wwwroot/js/microphone.js` to improve real-time frame delivery:
  - disabled browser voice-processing effects for piano capture
  - ensured `AudioContext` resume on start
  - connected analyser to a muted pull node to keep processing graph active across browsers
  - increased frame polling cadence for more responsive updates
  - added defensive client logging around frame invoke/read failures
- Improved calibration visibility in `Calibration.razor`:
  - when detection is present but not yet reliable, UI now shows `<note> (unreliable)` instead of always `No reliable note`
  - keeps note-light feedback visible during threshold tuning

### Expected user-visible result

- On calibration start, frequency/RMS/confidence should now move while real piano notes are played.
- On-screen key highlight should appear as soon as note candidates are observed, then stabilize as reliable detections are reached.

## 36. Progress Update (Calibration Listening Fix: JS->.NET Frame Binding)

### Root cause found

- Calibration values stayed at zero because microphone frame payload binding was not robust across interop serializer naming behavior.
- Result: `MicrophoneFrameDto` could receive empty/default data, so detector always returned unreliable zero values.

### Completed in this step

- Fixed JS microphone frame payload in `wwwroot/js/microphone.js` to emit DTO-aligned names during interop calls.
- Hardened DTO binding in `Models/MicrophoneFrameDto.cs` with explicit JSON property mapping for `samples` and `sampleRate`.
- Added dynamic frame diagnostics in `Calibration.razor` (`Frames received`, `Last sample rate`, `Last frame UTC`) to verify live capture flow instantly.

### Expected user-visible behavior

- After starting microphone on `/calibration`, frame counters should increment continuously.
- Frequency/RMS/confidence should update when real piano notes are played.
- On-screen key highlighting should now reflect detected note candidates and reliable notes.

## 37. Progress Update (Mic Frequency Not Updating + Better Piano Note Identification)

### Root cause fixed

- Calibration frequency staying at `0.00 Hz` was caused by JS microphone frame payload naming mismatch after prior edits.
- `OnAudioFrame` now receives correctly mapped frame data again.

### Completed in this step

- Fixed JS frame payload in `wwwroot/js/microphone.js` to send `samples` and `sampleRate` matching DTO JSON mapping.
- Improved pitch detection quality for real piano in `SimplePitchDetector`:
  - added signal conditioning (`DC offset removal + Hann window`) before autocorrelation
  - added fundamental-selection logic to reduce harmonic/octave misidentification
  - confidence now uses corrected-lag correlation score

### Expected gameplay impact

- Calibration frequency and confidence should move while playing real piano notes.
- Game note recognition should be more stable and closer to the real key being played.
- On-screen key lighting should align better with live piano input.

## 38. Progress Update (Calibration Microphone Selector + Saved Selection)

### Completed in this step

- Added a microphone input list box to `Calibration.razor`.
- Implemented dynamic microphone device enumeration through `IMicrophoneService`.
- Added persisted microphone selection support (saved in browser storage and restored on load).
- Updated capture startup so selected input device is used when starting microphone capture.

### Technical changes

- `Neurithm.Audio/IMicrophoneService.cs`
  - added `MicrophoneInputDevice` record
  - added `GetInputDevicesAsync()`
  - added `SetInputDeviceAsync(string? deviceId)`
  - added `SelectedInputDeviceId` property
- `Neurithm.Web/Services/WebMicrophoneService.cs`
  - implemented input device enumeration and persisted selected device retrieval/storage
  - passed selected device into JS capture startup
- `Neurithm.Web/wwwroot/js/microphone.js`
  - added `getInputDevices`, `saveInputDevice`, `getSavedInputDevice`
  - updated `startCapture` to accept and apply selected `deviceId`
- `Neurithm.Web/Pages/Calibration.razor`
  - rendered microphone list box
  - loads available devices dynamically
  - saves selected device and reuses it for capture

## 39. Progress Update (Shared-First Pitch Filtering: Low-Pass + Piano-Range Gate)

### Completed in this step

- Implemented shared/runtime filtering helpers inside the detector pipeline and kept feature flow as orchestration:
  - one-pole low-pass prefilter for incoming mic signal
  - bounded piano-range gate with semitone margin
  - between-key rejection using configurable cents distance from nearest key
- Extended detector options to keep behavior dynamic/configurable without hardcoded gameplay assumptions.

### Technical changes

- `Neurithm.Audio/IPitchDetector.cs`
  - added `LowPassCutoffHz`
  - added `PianoRangeMarginSemitones`
  - added `MaxCentsFromNearestNote`
- `Neurithm.Audio/SimplePitchDetector.cs`
  - added `ApplyOnePoleLowPassInPlace(...)`
  - added piano-range + between-key filtering using nearest MIDI note + cents bounds
  - kept core gameplay note recognition orchestration thin and dynamic through options

### Expected result

- Better rejection of non-piano noise and unstable harmonic content.
- More accurate key recognition from real piano input with fewer false positives.

## 40. Progress Update (Shared-First Averaging + Bounded Materializer for Stable Note Detection)

### Problem addressed

- Real piano input could fluctuate too fast (many note changes per second) under noise/turbulence (e.g., blowing air).
- Needed stable averaging and bounded output instead of per-frame note flipping.

### Completed in this step

- Implemented shared/runtime stabilization helpers in `SimplePitchDetector`:
  - temporal sample buffer for recent detections
  - dominant-note vote in smoothing window
  - averaged frequency/confidence/cents materialization for bounded output
- Added stricter bounded gates:
  - minimum autocorrelation peak threshold
  - existing low-pass + piano-range + between-key cent bounds retained
- Kept feature layer skinny and centralized orchestration in page/game layers.

### Dynamic runtime options added

- `MinCorrelationPeak`
- `SmoothingWindowMilliseconds`
- `FrameIntervalMilliseconds`
- `MinSamplesForAveraging`
- `DominantNoteVoteThreshold`

### Calibration integration

- `Calibration.razor` now applies the new options through `PitchDetector.UpdateOptions(...)` to keep tuning centralized and dynamic.

### Expected result

- Far fewer rapid note flips.
- More stable note lock for real piano keys.
- Better resilience to short bursts of noise/wind on mic.

## 41. Progress Update (Mic-Detected Piano Key Lighting)

### Completed in this step

- Updated shared piano keyboard component so real microphone detections visibly light up the matching piano key.
- Added dedicated `mic-detected` visual state with stronger white/black key styling and transition for fast refresh visibility.
- Updated calibration note handling so active key highlight clears when no valid current note exists, preventing stale key glow.

### Files updated

- `Neurithm.Web/Components/PianoKeyboard.razor`
  - unified dynamic mic-detection class application (`mic-detected`)
  - stronger key-on visual feedback for both white and black keys
- `Neurithm.Web/Pages/Calibration.razor`
  - active note reset behavior improved for non-detected frames

### Expected result

- When mic detects a note, the corresponding `<div class="piano-key white-key ...">` or black key lights up immediately.
- Key highlight follows live note state and clears correctly when detection drops.

## 42. Progress Update (Responsive Fit-to-Container Hardening Across Pages)

### Completed in this step

- Implemented global responsive guards to prevent horizontal overflow and keep pages within viewport.
- Hardened main layout containers so content fits screen width on desktop/mobile.
- Updated game page responsive behavior:
  - controls wrap safely
  - corner mic panel collapses into normal flow on smaller screens
  - visual host and panels clamp to container width

### Files updated

- `Neurithm.Web/wwwroot/css/app.css`
  - global `box-sizing` normalization
  - viewport overflow protection (`overflow-x: hidden`)
  - safe media sizing and long-content wrapping
- `Neurithm.Web/Layout/MainLayout.razor.css`
  - responsive width/overflow constraints for page/main/top-row/sidebar
- `Neurithm.Web/Pages/Game.razor`
  - fit-to-container responsive CSS and mobile breakpoints

### Shared-first note-detection status

- Shared filtering pipeline remains active from prior steps:
  - low-pass filtering
  - out-of-piano range rejection with margin
  - between-key cents rejection
  - averaged bounded materialization to reduce rapid noisy flips

## 43. Progress Update (Hit Timing Fix + No-Gap Piano Layout)

### Problem addressed

- User played correct notes on time but game still registered misses.
- Game piano visual had gaps/holes and needed calibration-style continuous look.

### Completed in this step

- Implemented gameplay timing compensation for mic-detection latency:
  - added bounded compensation window in `GameSessionService` so reliable detected notes are matched against compensated song-time.
  - miss resolution now waits for compensated window before marking unresolved notes as miss.
- Rebuilt `PianoKeyboard` rendering to eliminate holes:
  - white-key base row with black-key absolute overlay layout
  - dynamic key positioning from note list
  - retained mic-detected highlighting and target styling
  - mobile-friendly responsive sizing

### Shared-first alignment

- Feature flow remains skinny orchestration in page/game layers.
- Runtime/shared logic handles timing math and bounded materialization behavior.

### Expected result

- Correctly timed real-note hits should score instead of being incorrectly marked miss.
- Piano now appears continuous and visually improved (no holes) while still lighting the detected note.

## 44. Progress Update (Smooth Falling Notes + In-Game Perfect Visual Feedback)

### Problem addressed

- Falling notes looked laggy.
- Play/Pause/Resume controls needed to be lower.
- In-game real-note lighting needed to stay aligned and visible.
- User requested clear PERFECT feedback above correctly hit notes with pink/blue style and light-purple zigzag outline.

### Completed in this step

- `FallingNotes.razor`
  - Refactored render path to shared helper-style orchestration:
    - `ShouldRenderNote(...)`
    - `BuildNoteCss(...)`
    - `BuildNoteStyle(...)`
    - `IsPerfectFeedback(...)`
  - Switched note motion styling to transform-based positioning (`translate3d`) with `will-change` for smoother rendering.
  - Added per-note `PERFECT` badge above perfect-resolved notes.
  - Styled badge with pink + blue gradient and light-purple zigzag-style border effect.
- `Game.razor`
  - Moved top control row lower (`top-controls-row` spacing).
  - Unified live mic-lit key source with `liveDetectedNote` for both sync keyboard and gameplay keyboard.
  - Microphone status panel now also reflects `liveDetectedNote` directly.
- `GameSessionService` from prior step remains in effect with timing compensation to reduce false misses.

### Expected result

- Falling notes should render more smoothly.
- Buttons appear lower and cleaner in the layout.
- Correctly timed real-note hits show strong visual confirmation, including `PERFECT` above the note.
- In-game piano key lighting tracks live reliable detection more consistently.

## 45. Progress Update (Single In-Game Piano + Smoother Fall + Lower Controls/HUD)

### Problem addressed

- Game had two piano sections; needed one unified piano in gameplay.
- Falling notes appeared laggy and sometimes looked like they stopped mid-screen.
- Controls/HUD needed to sit lower for better visibility while playing.
- Detected note lighting needed to remain active in-game.

### Completed in this step

- `Game.razor`
  - Removed duplicate in-game sync piano block and kept one gameplay piano only.
  - Kept live note lighting source unified from `liveDetectedNote`.
  - Lowered controls area and mic corner panel placement.
  - Added responsive score-panel offset variable (`--score-panel-top`) for better in-play visibility.
- `ScorePanel.razor`
  - Sticky offset is now dynamic via CSS variable (page-controlled).
  - Added responsive grid collapse for narrow screens.
- `GameSessionService.cs`
  - Smoothed falling note motion with exact helper math:
    - eased progress (`EaseOutCubic`)
    - extended approach window
    - broader travel range to bottom hit region
  - Hit-window visualization now uses compensated timing reference.

### Shared-first alignment

- Exact movement and timing math live in runtime service/helpers.
- UI remains skinny orchestration over snapshot values.

### Expected result

- One piano only in game.
- Notes travel smoothly through the full board and align better with hit area.
- Play/Pause/Resume and HUD are lower and more visible while playing.
- Detected note highlight stays active on the in-game piano.

## 46. Progress Update (Single-Piano Gameplay Visibility + Faster Note Lighting)

### Problem addressed

- Real piano note lighting in game still felt unresponsive.
- Gameplay controls/HUD required further lowering/compaction to avoid constant scrolling.
- Keep game on a single piano layout.

### Completed in this step

- `Game.razor`
  - Kept single in-game piano (no duplicate keyboard sections).
  - Compact/lower gameplay chrome for always-visible play context:
    - reduced title spacing
    - lowered controls row
    - lowered HUD anchor via `--score-panel-top`
    - lowered mic panel top
    - reduced Three.js visual host height to keep interactive area in view
- `IPitchDetector` defaults tuned for faster visible note-light response while retaining bounded filtering:
  - lower RMS and confidence thresholds
  - shorter debounce and smoothing window
  - lower minimum averaging sample count
  - lower dominant vote threshold
  - slightly lower correlation peak threshold

### Shared-first alignment

- Exact filtering and bounded materialization remain centralized in runtime detector helpers.
- UI stays skinny orchestration over detector/session outputs.

### Expected result

- In-game detected note lighting should react faster to real piano input.
- One piano only in game.
- Less vertical scrolling during gameplay; controls/HUD stay lower and visible.
