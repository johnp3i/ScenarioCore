# ScenarioCore — Clip Planning Algorithm Specification (V1)

## Purpose

This document defines the **Clip Planning Algorithm** for ScenarioCore V1.

The clip planning algorithm converts a selected simulation path into an **ordered render plan** that can be submitted to the video provider (Runway) as individual clip-generation tasks.

This is a core engine artifact because it determines:

- how a script becomes renderable
- how branch choices affect the final sequence
- how many clips must be generated
- how cost is estimated before submission
- how the final MP4 can be assembled deterministically

---

# 1. Why This Algorithm Exists

Runway does not render a 60–90 second simulation in one request.

Instead, it renders **short clips**.

Therefore, ScenarioCore must transform:

**Script + BranchPath + Duration Rules**

into:

**Ordered Clip Plan**

That clip plan is the bridge between narrative logic and video generation.

---

# 2. V1 Scope

This specification is for **Version 1** only.

## Included
- single-user session
- one decision node
- one selected branch path
- one simulation template
- one script
- one render job
- clip-based rendering for 60–90 seconds
- deterministic output order

## Excluded
- multi-user branch voting
- branch pre-generation for multiple options
- dynamic clip duration per option based on emotion complexity
- adaptive scene trimming by analytics
- reusable clip cache optimization
- cinematic continuity memory across jobs

---

# 3. Core Inputs

The algorithm receives the following inputs:

## 3.1 Script
A structured generated script containing:
- scenes
- scene descriptions
- dialogue
- one or more decision nodes
- decision options linked to next scene indexes

## 3.2 Branch Path
A deterministic representation of choices.

Example:
`D1:A`

This means:
- Decision Node 1
- Option A selected

## 3.3 Render Strategy
V1 default render strategy:
- target total duration: 60–90 seconds
- target clip duration: 8 seconds
- minimum clip duration: 5 seconds
- maximum clip duration: 10 seconds

## 3.4 Style Profile
Used to shape prompts but not the algorithmic order.

Example:
- anime cinematic
- office training
- realistic shadows
- warm lighting

---

# 4. Core Outputs

The algorithm produces a **Clip Plan**.

A Clip Plan is an ordered list of clip instructions.

Each clip instruction contains:
- ClipIndex
- SceneIndex
- BranchPathHash
- StartBeat
- EndBeat
- PromptSeed
- EstimatedDurationSeconds
- RenderIntent

Example output:
- Clip 1 → Scene 1 → Setup
- Clip 2 → Scene 2 → Conflict
- Clip 3 → Scene 2 → Decision build-up
- Clip 4 → Scene 3 → Branch A outcome

---

# 5. Design Principles

## 5.1 Determinism
The same:
- Script
- BranchPath
- Render strategy

must always produce the same clip plan.

## 5.2 Stable Ordering
Clips must always be generated in a stable order.

## 5.3 Cost Predictability
Before provider submission, the system must know:
- number of clips
- estimated total render seconds
- estimated provider cost

## 5.4 Render Simplicity
V1 will not try to solve cinematic perfection.
It will solve:
- ordered generation
- branch correctness
- assembly readiness

---

# 6. Narrative Planning Model

The algorithm works in **beats**, not only scenes.

A scene may become:
- one clip
- multiple clips

A beat is the smallest narrative planning unit in V1.

## Beat Types
- Setup
- Context
- Tension
- DecisionBuild
- Outcome
- Transition
- Closure

Example:
A single “phone rings” scene might include:
- Setup beat
- Tension beat
- DecisionBuild beat

This is important because scenes are often too coarse to map directly to clips.

---

# 7. V1 Clip Planning Rules

## Rule 1 — Build the Linear Path First
Starting from scene 1, follow the script in order.

When a decision node is reached:
- apply the selected option from BranchPath
- continue only through the chosen branch

This creates the **Resolved Narrative Path**.

## Rule 2 — Expand Scenes into Beats
Each scene in the resolved path is expanded into one or more beats.

### Default Beat Expansion Heuristics

#### Introductory scene
Produces:
- Setup

#### Dialogue-heavy scene
Produces:
- Context
- Tension

#### Pre-decision scene
Produces:
- Tension
- DecisionBuild

#### Branch-result scene
Produces:
- Outcome

#### Final scene
Produces:
- Closure

## Rule 3 — Group Beats into Clips
Beats are grouped into clips according to duration strategy.

Default V1 target:
- 1 clip ≈ 8 seconds

Grouping rules:
- 1 short beat may stand alone
- 2 compatible short beats may be merged into one clip
- a long beat may become its own clip

## Rule 4 — Preserve Narrative Order
A later beat may never appear before an earlier beat.

## Rule 5 — Branch Outcome Must Affect Downstream Clips
Once a branch is selected, all subsequent beats must come only from that branch path.

---

# 8. Resolved Narrative Path

## Definition
The resolved narrative path is the exact ordered list of scenes after decisions are applied.

### Example Script
- Scene 1 → Setup
- Scene 2 → Phone rings / decision node
- Option A → Scene 3
- Option B → Scene 4
- Scene 5 → ending

### BranchPath = D1:A

Resolved path:
- Scene 1
- Scene 2
- Scene 3
- Scene 5

### BranchPath = D1:B

Resolved path:
- Scene 1
- Scene 2
- Scene 4
- Scene 5

This resolved path is the first algorithmic transformation.

---

# 9. Beat Expansion Strategy

V1 uses deterministic beat expansion rules.

## Example Mapping

### Scene Type: Introduction
Beats:
- Setup

### Scene Type: Environment + Suspense
Beats:
- Context
- Tension

### Scene Type: Decision Preparation
Beats:
- Tension
- DecisionBuild

### Scene Type: Branch Result
Beats:
- Outcome

### Scene Type: Ending
Beats:
- Closure

---

# 10. Clip Grouping Strategy

## V1 Target
- target clip duration = 8 seconds
- target total duration = 60–90 seconds

## Clip Count Formula
`EstimatedClipCount = Ceiling(TargetTotalDuration / TargetClipDuration)`

Examples:

### 60 seconds
`Ceiling(60 / 8) = 8 clips`

### 90 seconds
`Ceiling(90 / 8) = 12 clips`

---

# 11. V1 Planning Heuristic

V1 uses a simple planning heuristic:

1. Resolve path
2. Expand scenes into beats
3. Estimate weight per beat
4. Group beats into clips until target duration is reached
5. Preserve order
6. Emit clip plan

---

# 12. Beat Weighting

Each beat has a weight that influences clip allocation.

## Suggested Weights

- Setup = 1
- Context = 1
- Tension = 2
- DecisionBuild = 2
- Outcome = 2
- Transition = 1
- Closure = 1

These weights help decide which scenes deserve more visual time.

### Example
A tension-heavy sequence should get more screen time than a simple transition.

---

# 13. Clip Allocation Algorithm

## Step-by-Step

### Step 1
Resolve branch path into a final scene list.

### Step 2
Expand each scene into beats.

### Step 3
Calculate total beat weight.

### Step 4
Compute proportional duration budget for each beat.

### Step 5
Merge or split beats into clips within:
- min = 5 sec
- target = 8 sec
- max = 10 sec

### Step 6
Assign each clip:
- ClipIndex
- SceneIndex
- Beat range
- PromptSeed
- Duration

---

# 14. Minimal Pseudocode

```text
input: script, branchPath, targetDuration=60, clipTarget=8

resolvedScenes = ResolveScenes(script, branchPath)

beats = []
for scene in resolvedScenes:
    beats += ExpandSceneToBeats(scene)

totalWeight = Sum(beats.Weight)

for beat in beats:
    beat.DurationBudget = Round((beat.Weight / totalWeight) * targetDuration)

clips = GroupBeatsIntoClips(beats, min=5, target=8, max=10)

return BuildClipPlan(clips)
```

---

# 15. Prompt Seed Generation

Each clip must produce a prompt seed for the video provider.

## Prompt Seed Components
- style profile
- environment
- characters
- beat type
- emotional state
- action summary
- camera intent

### Example
`Anime cinematic style, dim apartment at night, rotary phone on wooden table, man freezes in tension, slow push-in, warm lamp light, suspense mood.`

This prompt seed is later expanded by the rendering service.

---

# 16. Clip Plan Structure

Recommended V1 structure:

```json
{
  "renderJobId": "GUID",
  "branchPath": "D1:A",
  "targetDurationSeconds": 60,
  "clips": [
    {
      "clipIndex": 1,
      "sceneIndex": 1,
      "beatType": "Setup",
      "durationSeconds": 8,
      "promptSeed": "..."
    }
  ]
}
```

---

# 17. Cost Estimation

The clip planning algorithm must return estimated cost before execution.

## Formula
`EstimatedCost = Sum(Clip.DurationSeconds × ModelCreditsPerSecond × CreditUnitCost)`

Example with gen4.5:
- 8 clips × 8 sec = 64 sec
- 64 × 12 credits = 768 credits
- 768 × $0.01 = $7.68 raw

This estimate is essential before creating the render job.

---

# 18. V1 Example

## Script
- Scene 1: Man alone in apartment
- Scene 2: Phone rings, tension rises, decision node
- Option A: Answer phone → Scene 3
- Option B: Ignore → Scene 4
- Scene 5: Ending

## BranchPath
`D1:A`

## Resolved Scenes
- Scene 1
- Scene 2
- Scene 3
- Scene 5

## Expanded Beats
- Scene 1 → Setup
- Scene 2 → Context
- Scene 2 → Tension
- Scene 2 → DecisionBuild
- Scene 3 → Outcome
- Scene 5 → Closure

## Clip Plan (Example)
- Clip 1 → Scene 1 / Setup
- Clip 2 → Scene 2 / Context + Tension
- Clip 3 → Scene 2 / DecisionBuild
- Clip 4 → Scene 3 / Outcome
- Clip 5 → Scene 5 / Closure

For V1, the planner may add visual breathing clips or transitions to reach 60 seconds.

---

# 19. Edge Cases

## 19.1 Too Few Beats
If the resolved path produces too few beats:
- extend by splitting tension/outcome beats
- add transition beats
- preserve narrative consistency

## 19.2 Too Many Beats
If too many beats exceed target duration:
- merge low-weight beats
- shorten transition beats first

## 19.3 Missing Branch
If BranchPath does not resolve:
- fail planning
- do not create render job

## 19.4 Invalid NextSceneIndex
If branch links are broken:
- fail validation earlier in script persistence
- planner must not guess

---

# 20. Responsibilities by Layer

## Domain
Defines:
- scenes
- decisions
- branch semantics

## Application
Executes:
- clip planning service
- cost estimation
- render plan creation

## Infrastructure
Uses clip plan to:
- generate provider requests
- persist outputs

The planner belongs in the **Application layer**, not Infrastructure.

---

# 21. Recommended First Implementation

## First Implementable Version
Implement the simplest working planner:

1. resolve path
2. convert each scene to 1–2 beats using static rules
3. convert beats to 1 clip each
4. assign fixed duration = 8 sec
5. estimate cost
6. persist clip plan as RenderClips

This is enough for V1.

Do not over-engineer cinematic timing yet.

---

# 22. Exit Criteria

The Clip Planning Algorithm is considered complete for V1 when:

- a stored script can be resolved through a chosen branch path
- the resolved narrative becomes an ordered list of clips
- each clip has a deterministic prompt seed
- clip count is known before rendering
- estimated render cost is known before submission
- the output can be persisted as RenderClips

---

# 23. Immediate Next Step in Execution Plan

After this algorithm, the correct next step is:

**Domain Model Refinement aligned to clip planning**

That ensures:
- RenderJob
- RenderClip
- BranchPath
- Scene relationships

are all modeled correctly before persistence mapping.
