# ScenarioCore — Clip Planning Service Implementation (V1, KIRO-Ready)

## Purpose
Deterministic engine that converts:
Script + BranchPath → Clip Plan → Cost Estimate

---

## Interface

public interface IClipPlanner
{
    ClipPlanningResult Plan(ClipPlanningRequest request);
}

---

## Execution Flow

1. Validate request
2. Parse branch path
3. Resolve scene path
4. Expand scenes → beats
5. Assign weights
6. Allocate durations
7. Build clips (1 beat = 1 clip)
8. Generate prompt seeds
9. Estimate cost

---

## Key Constraints

- No DB access
- No external APIs
- Deterministic output
- SHA256 hash for BranchPath
- Normalize total duration to target

---

## ClipPlanItem

- ClipIndex
- SceneIndex
- BeatType
- PlannedDurationSeconds
- PromptSeed

---

## Cost

Credits = seconds * modelRate  
Cost = credits * 0.01

---

## Integration

Used by:
CreateRenderJobUseCase

---

## Exit Criteria

- stable output
- deterministic behavior
- unit tested
