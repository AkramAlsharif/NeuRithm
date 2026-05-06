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
