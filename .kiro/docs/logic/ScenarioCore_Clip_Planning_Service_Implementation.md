# ScenarioCore — Clip Planning Service Implementation (V1, KIRO-Ready)

## Purpose

This document defines the **implementation blueprint** for the Clip Planning Service in ScenarioCore V1.

This is not a high-level concept note.  
It is an implementation-ready specification for the first working version of the clip planner.

The Clip Planning Service is responsible for converting:

- a generated script
- a selected session path
- a deterministic render strategy

into:

- a stable ordered clip plan
- estimated duration allocation
- prompt seeds
- estimated render cost
- `RenderClip` records ready for persistence

---

# 1. Why This Service Matters

This service is one of the most important application-layer components in ScenarioCore.

It determines:

- how narrative becomes renderable
- how many provider calls are required
- how much a render will cost
- how branch choices affect output
- how final assembly order is preserved

If this service is incorrect, the entire render pipeline becomes unstable.

---

# 2. Layer Placement

## Correct Layer
The Clip Planning Service belongs in:

`ScenarioCore.Application`

## Why
Because it is:

- orchestration logic
- deterministic business logic
- provider-agnostic
- dependent on domain objects
- independent of HTTP or Runway specifics

## It must NOT live in:
- `ScenarioCore.Domain`
- `ScenarioCore.Infrastructure`
- `ScenarioCore.Api`

---

# 3. Responsibilities

The service must:

1. resolve the selected branch path
2. produce a resolved ordered scene list
3. expand scenes into beats
4. assign beat weights
5. allocate clip durations
6. group beats into ordered clips
7. generate prompt seeds
8. estimate provider cost
9. return a stable result for persistence into `RenderJob` / `RenderClip`

The service must NOT:
- call Runway
- persist data directly
- create HTTP responses
- perform blob storage operations

---

# 4. Service Interface

## Interface

```csharp
public interface IClipPlanner
{
    ClipPlanningResult Plan(ClipPlanningRequest request);
}
```

## Why synchronous?
For V1, the planner is pure deterministic in-memory logic.
It does not perform I/O.
So synchronous execution is correct.

If in future prompt enrichment requires external services, the interface can evolve.

---

# 5. Input Model

## ClipPlanningRequest

```csharp
public sealed class ClipPlanningRequest
{
    public Guid ScriptId { get; init; }
    public string BranchPathRaw { get; init; } = default!;
    public int TargetDurationSeconds { get; init; }
    public int TargetClipDurationSeconds { get; init; } = 8;
    public int MinClipDurationSeconds { get; init; } = 5;
    public int MaxClipDurationSeconds { get; init; } = 10;
    public string StyleProfile { get; init; } = default!;
    public string RenderModel { get; init; } = "gen4.5";

    public IReadOnlyList<ScriptSceneDto> Scenes { get; init; } = Array.Empty<ScriptSceneDto>();
    public IReadOnlyList<DecisionNodeDto> DecisionNodes { get; init; } = Array.Empty<DecisionNodeDto>();
}
```

### Notes
- V1 assumes all required data is already loaded by the caller.
- The planner must not query the database itself.
- This keeps it pure and testable.

---

# 6. Internal DTOs for Planning

## ScriptSceneDto

```csharp
public sealed class ScriptSceneDto
{
    public int SceneIndex { get; init; }
    public string SceneDescription { get; init; } = default!;
    public string? DialogueJson { get; init; }
    public SceneType SceneType { get; init; }
}
```

## DecisionNodeDto

```csharp
public sealed class DecisionNodeDto
{
    public int SceneIndex { get; init; }
    public string Prompt { get; init; } = default!;
    public IReadOnlyList<DecisionOptionDto> Options { get; init; } = Array.Empty<DecisionOptionDto>();
}
```

## DecisionOptionDto

```csharp
public sealed class DecisionOptionDto
{
    public string OptionKey { get; init; } = default!;
    public string OptionText { get; init; } = default!;
    public int NextSceneIndex { get; init; }
}
```

---

# 7. Output Model

## ClipPlanningResult

```csharp
public sealed class ClipPlanningResult
{
    public string BranchPathRaw { get; init; } = default!;
    public string BranchPathHash { get; init; } = default!;
    public int TargetDurationSeconds { get; init; }
    public int PlannedDurationSeconds { get; init; }
    public int EstimatedClipCount { get; init; }
    public decimal EstimatedCredits { get; init; }
    public decimal EstimatedCostUsd { get; init; }
    public IReadOnlyList<ClipPlanItem> Clips { get; init; } = Array.Empty<ClipPlanItem>();
}
```

## ClipPlanItem

```csharp
public sealed class ClipPlanItem
{
    public int ClipIndex { get; init; }
    public int SceneIndex { get; init; }
    public BeatType BeatType { get; init; }
    public int PlannedDurationSeconds { get; init; }
    public string PromptSeed { get; init; } = default!;
}
```

---

# 8. Helper Models (Internal)

These internal models should stay inside the Application layer.

## ResolvedScene

```csharp
internal sealed class ResolvedScene
{
    public int SceneIndex { get; init; }
    public string SceneDescription { get; init; } = default!;
    public SceneType SceneType { get; init; }
}
```

## PlannedBeat

```csharp
internal sealed class PlannedBeat
{
    public int SceneIndex { get; init; }
    public BeatType BeatType { get; init; }
    public int Weight { get; init; }
    public string ActionSummary { get; init; } = default!;
    public int DurationBudgetSeconds { get; set; }
}
```

---

# 9. Execution Flow

The service must execute the following exact sequence:

1. validate request
2. parse branch path
3. resolve final scene path
4. expand scenes into beats
5. assign beat weights
6. allocate duration budgets
7. normalize durations to fit target constraints
8. convert beats into clip plan items
9. generate prompt seeds
10. estimate credits/cost
11. return result

This order must not be changed casually.

---

# 10. Validation Rules

The planner must fail fast if:

- target duration <= 0
- target clip duration is outside min/max range
- scenes list is empty
- branch path is empty
- branch path references unknown decision
- selected option does not exist
- resolved next scene does not exist
- no clips can be produced

## Recommended Exception
```csharp
public sealed class ClipPlanningException : Exception
{
    public ClipPlanningException(string message) : base(message) { }
}
```

---

# 11. Branch Path Parsing

## V1 Format
Example:
`D1:A`

Future:
`D1:A|D2:C`

## Parsing Rule
Each segment:
- starts with `D`
- contains `:`
- left part = decision sequence number
- right part = option key

## Parsed Result
Internally convert to:
```csharp
Dictionary<int, string>
```

Example:
```csharp
{ 1 => "A" }
```

## Recommendation
Keep parsing strict. Do not attempt to auto-correct malformed branch paths.

---

# 12. Resolved Scene Path Algorithm

## Goal
Build the exact ordered scene list after applying branch choices.

## V1 Assumption
- scenes are stored in ascending order
- decision nodes are attached to scenes by `SceneIndex`
- each decision option points to `NextSceneIndex`

## Algorithm

### Step 1
Start at the first scene (lowest SceneIndex).

### Step 2
Add scenes in sequence.

### Step 3
If current scene has a decision node:
- find selected option from branch path
- jump to `NextSceneIndex`
- continue from there

### Step 4
Stop when no more scenes remain.

## Pseudocode

```text
sceneMap = scenes ordered by SceneIndex
currentSceneIndex = first scene index
resolved = []

while currentSceneIndex exists:
    scene = sceneMap[currentSceneIndex]
    resolved.add(scene)

    if scene has decision node:
        selectedOption = branch path choice for that decision
        currentSceneIndex = selectedOption.NextSceneIndex
    else:
        currentSceneIndex = next sequential scene index
```

## Important
To prevent loops, maintain a visited set and fail if the same scene is revisited unexpectedly.

---

# 13. Scene → Beat Expansion Rules

V1 uses deterministic scene expansion.

## Mapping Table

### Introduction
Produces:
- Setup

### Context
Produces:
- Context

### Suspense
Produces:
- Tension

### Decision
Produces:
- Tension
- DecisionBuild

### Outcome
Produces:
- Outcome

### Ending
Produces:
- Closure

## Implementation Method

```csharp
private static IReadOnlyList<PlannedBeat> ExpandSceneToBeats(ResolvedScene scene)
```

## Example

SceneType = Decision  
Output:
- Tension
- DecisionBuild

---

# 14. Beat Weighting Rules

Each beat receives an integer weight.

## V1 Weight Table

| BeatType | Weight |
|---|---:|
| Setup | 1 |
| Context | 1 |
| Tension | 2 |
| DecisionBuild | 2 |
| Outcome | 2 |
| Transition | 1 |
| Closure | 1 |

## Why
This gives more visual time to dramatic and branch-relevant beats.

## Implementation Method

```csharp
private static int GetWeight(BeatType beatType)
```

---

# 15. Duration Allocation Algorithm

## Goal
Distribute the target total duration across planned beats.

## Step 1
Compute total beat weight.

## Step 2
For each beat:
```text
beat.DurationBudget = round((beat.Weight / totalWeight) * targetDuration)
```

## Step 3
Enforce minimum of 1 second at beat level before grouping.

## Step 4
Normalize sum to target duration.

## Normalization Strategy
After rounding, the total may differ from target.

### If total is too low
Add 1 second at a time to highest-weight beats until target is reached.

### If total is too high
Subtract 1 second at a time from lowest-priority beats, but never below 1 second, until target is reached.

---

# 16. Clip Grouping Strategy

## V1 Rule
For V1, use the simplest stable grouping:

- one beat = one clip initially
- if a beat duration exceeds `MaxClipDurationSeconds`, split it
- if a beat duration is below `MinClipDurationSeconds`, allow it in V1 rather than over-optimizing
- optionally merge very short adjacent low-weight beats only if they belong to the same scene and total stays <= max

## Recommendation for first implementation
Keep it simple:
- **1 beat = 1 clip**
- use fixed clip duration = `TargetClipDurationSeconds`
- adjust only if total target would be exceeded significantly

This is the correct V1 implementation trade-off.

## Why
The planner must be correct before it becomes clever.

---

# 17. Prompt Seed Generation

## Goal
Produce a provider-agnostic prompt seed per clip.

## Prompt Seed Components
Each prompt seed should include:

- style profile
- environment summary from scene description
- beat action
- emotional tone
- camera direction hint

## Template

```text
{StyleProfile}. {SceneDescription}. {BeatAction}. Cinematic framing. Consistent tone. 16:9.
```

## Example
```text
Anime cinematic style. Dim living room at night with rotary phone on wooden table. A man freezes as tension rises before making a decision. Cinematic framing. Consistent tone. 16:9.
```

## Implementation Method
```csharp
private static string BuildPromptSeed(string styleProfile, ResolvedScene scene, PlannedBeat beat)
```

---

# 18. Beat Action Summaries

Each beat type should map to a stable action phrase.

## Suggested Mapping

| BeatType | Action Summary |
|---|---|
| Setup | Character is introduced in the environment |
| Context | Environment and situation become clear |
| Tension | Tension rises and focus sharpens |
| DecisionBuild | Character hesitates before choosing |
| Outcome | Consequence of the choice becomes visible |
| Transition | Scene shifts toward the next moment |
| Closure | Final emotional resolution settles |

## Implementation Method
```csharp
private static string GetActionSummary(BeatType beatType)
```

---

# 19. Cost Estimation

## Goal
Return deterministic cost estimates before render submission.

## Formula

```text
EstimatedCredits = Sum(Clip.PlannedDurationSeconds * CreditsPerSecond(RenderModel))
EstimatedCostUsd = EstimatedCredits * 0.01
```

## Example Model Rates for V1
These should not be hardcoded in the planner forever, but V1 can use:

- gen4.5 = 12 credits/sec
- gen3a_turbo = 5 credits/sec

## Implementation Method
```csharp
private static decimal GetCreditsPerSecond(string model)
```

---

# 20. Recommended Class Structure

## Files

```text
ScenarioCore.Application
│
├── ClipPlanning
│   ├── IClipPlanner.cs
│   ├── ClipPlanner.cs
│   ├── ClipPlanningRequest.cs
│   ├── ClipPlanningResult.cs
│   ├── ClipPlanItem.cs
│   ├── ScriptSceneDto.cs
│   ├── DecisionNodeDto.cs
│   ├── DecisionOptionDto.cs
│   └── ClipPlanningException.cs
```

---

# 21. Recommended Implementation Skeleton

## IClipPlanner

```csharp
public interface IClipPlanner
{
    ClipPlanningResult Plan(ClipPlanningRequest request);
}
```

## ClipPlanner

```csharp
public sealed class ClipPlanner : IClipPlanner
{
    public ClipPlanningResult Plan(ClipPlanningRequest request)
    {
        Validate(request);

        var branchPathHash = ComputeBranchPathHash(request.BranchPathRaw);
        var decisions = ParseBranchPath(request.BranchPathRaw);
        var resolvedScenes = ResolveScenes(request.Scenes, request.DecisionNodes, decisions);
        var beats = ExpandBeats(resolvedScenes);
        AllocateDurations(beats, request.TargetDurationSeconds);

        var clips = BuildClipPlanItems(
            beats,
            resolvedScenes,
            request.StyleProfile,
            request.TargetClipDurationSeconds);

        var estimatedCredits = EstimateCredits(clips, request.RenderModel);
        var estimatedCost = estimatedCredits * 0.01m;

        return new ClipPlanningResult
        {
            BranchPathRaw = request.BranchPathRaw,
            BranchPathHash = branchPathHash,
            TargetDurationSeconds = request.TargetDurationSeconds,
            PlannedDurationSeconds = clips.Sum(x => x.PlannedDurationSeconds),
            EstimatedClipCount = clips.Count,
            EstimatedCredits = estimatedCredits,
            EstimatedCostUsd = estimatedCost,
            Clips = clips
        };
    }
}
```

---

# 22. Deterministic Hashing

Use SHA256 on the exact raw branch path string.

## Example
```csharp
private static string ComputeBranchPathHash(string raw)
```

Return lowercase hex string length 64.

This must match persistence design.

---

# 23. Persistence Integration Point

The planner should be used during render request creation.

## Correct Flow

1. load session
2. load script + scenes + decisions
3. reconstruct branch path from decisions
4. call planner
5. create `RenderJob`
6. create `RenderClip` rows from `ClipPlanItem`
7. persist
8. worker processes later

## Important
The planner does not create `RenderClip` entities itself.
The Application use case does.

---

# 24. KIRO Implementation Tasks

## Task 1
Create Application/ClipPlanning folder and all contracts.

## Task 2
Implement `ClipPlanningException`.

## Task 3
Implement `ClipPlanner.Validate()`.

## Task 4
Implement branch path parsing.

## Task 5
Implement resolved scene path algorithm.

## Task 6
Implement scene → beat expansion.

## Task 7
Implement beat weighting.

## Task 8
Implement duration allocation and normalization.

## Task 9
Implement prompt seed construction.

## Task 10
Implement credits/cost estimation.

## Task 11
Write unit tests for:
- valid branch path
- invalid branch path
- missing option
- deterministic clip ordering
- deterministic hash
- cost estimation correctness

---

# 25. Unit Test Coverage Requirements

Create tests for at least the following scenarios:

## Test A — Simple Linear Path
No decision node.
Expected:
- scenes resolved sequentially
- clips produced in order

## Test B — Single Branch Path A
Decision path `D1:A`
Expected:
- correct next scene selected
- correct clip count

## Test C — Single Branch Path B
Decision path `D1:B`
Expected:
- different resolved scene path from A
- deterministic order

## Test D — Invalid Branch Path
Expected:
- `ClipPlanningException`

## Test E — Hash Stability
Same branch path twice → same hash

## Test F — Cost Stability
Same clip plan + same model → same estimated cost

---

# 26. V1 Simplification Rules

To keep V1 implementable:

- no adaptive cinematic timing
- no context memory across clips
- no semantic dialogue parsing
- no AI-based beat classification
- no external prompt enhancer
- no pre-render branch optimization

These can come later.

---

# 27. Exit Criteria

The Clip Planning Service is complete for V1 when:

- it accepts a loaded script/session planning request
- it resolves branch path deterministically
- it produces stable clip plan items
- it generates prompt seeds
- it estimates credits/cost
- it is fully unit tested
- it can drive `RenderJob` + `RenderClip` creation

---

# 28. Immediate Next Step After This

After implementing this service, the next correct step is:

**Create the actual ScenarioCore solution and project structure, then implement entities + EF configurations if not already done, or wire the planner into RenderJob creation if bootstrap is complete.**
