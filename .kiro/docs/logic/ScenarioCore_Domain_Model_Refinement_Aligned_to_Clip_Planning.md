# ScenarioCore — Domain Model Refinement Aligned to Clip Planning (V1)

## Purpose

This document refines the ScenarioCore V1 domain model after the Clip Planning Algorithm definition.

Its purpose is to ensure that:

- the domain reflects the real execution flow
- clip planning has explicit ownership
- render orchestration is modeled correctly
- branch paths and scenes are represented deterministically
- future persistence mapping can be done without structural ambiguity

This document is part of the ordered execution plan for ScenarioCore V1.

---

# 1. Why This Refinement Happens Now

Before the Clip Planning Algorithm, the domain could be modeled only at a high level.

After the Clip Planning Algorithm, we now know:

- scripts become resolved narrative paths
- resolved narrative paths become beats
- beats become clips
- clips become provider tasks
- provider tasks become render outputs

Therefore, the domain model must now explicitly support:

- branch path resolution
- clip planning inputs
- render plan creation
- clip persistence
- render lifecycle transitions

This refinement prevents us from creating entities that do not match the actual system behavior.

---

# 2. V1 Domain Scope

Version 1 includes only the minimum domain required to support:

**Template → Script → Session → Decision → BranchPath → Clip Plan → RenderJob → RenderClip → Final Video**

## Included
- predefined simulation template selection
- generated script
- one or more scenes
- one decision node
- one selected option
- one session
- one resolved branch path
- one render job
- multiple render clips

## Excluded
- subscriptions
- billing
- analytics
- psychometric scoring
- multi-user collaboration
- organization management
- clip cache reuse
- provider-specific state leakage into domain

---

# 3. Core Domain Concepts

The V1 domain is built around the following concepts:

## 3.1 Simulation Template
A predefined scenario definition chosen by the user.

## 3.2 Script
A generated narrative structure produced from a template.

## 3.3 Scene
A narrative unit inside a script.

## 3.4 Decision Node
A point inside the script where the user selects one of several options.

## 3.5 Decision Option
A concrete path choice that resolves to a next scene.

## 3.6 Simulation Session
An executable instance of a script in progress.

## 3.7 Branch Path
A deterministic representation of selected options.

## 3.8 Render Job
A request to convert a selected branch path into final video output.

## 3.9 Render Clip
A single renderable and trackable video segment belonging to a render job.

---

# 4. Aggregate Boundaries

The V1 domain should use the following aggregate boundaries.

## 4.1 Script Aggregate

### Aggregate Root
`Script`

### Child Entities
- `ScriptScene`
- `DecisionNode`
- `DecisionOption`

### Why
The script and its internal structure must remain consistent as one narrative definition.

### Invariants
- every decision node must belong to one script
- every decision option must belong to one decision node
- every option must resolve to a valid next scene index
- scenes inside a script must have stable ordering

---

## 4.2 Session Aggregate

### Aggregate Root
`SimulationSession`

### Child Entities
- `DecisionVote`

### Why
A session tracks the user’s execution of one script and the resulting branch path.

### Invariants
- a session belongs to exactly one script
- a decision node may be voted only once in V1
- branch path must be reproducible from stored decisions
- session status transitions must be valid

---

## 4.3 Render Aggregate

### Aggregate Root
`RenderJob`

### Child Entities
- `RenderClip`

### Why
A render job owns the execution state of a single selected narrative path.

### Invariants
- a render job belongs to exactly one session
- a render job is built from exactly one branch path
- render clips must have stable order
- render clips cannot belong to multiple render jobs
- render status transitions must be valid

---

# 5. Entity Definitions

# 5.1 SimulationTemplate

## Role
Represents a predefined scenario type.

## Responsibilities
- define scenario category
- constrain character count
- constrain allowed durations
- identify the scenario code used for script generation

## Suggested Fields
- SimulationTemplateId
- Code
- Title
- Description
- MaxCharacters
- AllowedDurationsJson
- Version
- Status

## Notes
For V1 this may remain seeded/static, but it is still a domain concept and should not be hardcoded across all layers forever.

---

# 5.2 Script

## Role
Represents one generated narrative structure for a selected template.

## Responsibilities
- own scenes
- own decision nodes
- maintain narrative consistency

## Suggested Fields
- ScriptId
- SimulationTemplateId
- DurationMinutes
- CharacterCount
- CreatedAt

## Key Behaviors
- add scene
- add decision node
- validate narrative structure

---

# 5.3 ScriptScene

## Role
Represents one ordered scene in the script.

## Responsibilities
- define narrative content for one stage of the story
- act as a source for beat expansion during clip planning

## Suggested Fields
- ScriptSceneId
- ScriptId
- SceneIndex
- SceneDescription
- DialogueJson
- SceneType

## Notes
`SceneType` is important because beat expansion uses scene classification.

### Suggested V1 SceneType values
- Introduction
- Context
- Suspense
- Decision
- Outcome
- Ending

---

# 5.4 DecisionNode

## Role
Represents a decision point in the script.

## Responsibilities
- define the prompt presented to the user
- define the valid options for progression

## Suggested Fields
- DecisionNodeId
- ScriptId
- SceneIndex
- Prompt

## Key Behaviors
- add option
- validate options
- expose allowed choices

---

# 5.5 DecisionOption

## Role
Represents one allowed branch from a decision node.

## Responsibilities
- define user-visible option text
- resolve to a next scene

## Suggested Fields
- DecisionOptionId
- DecisionNodeId
- OptionKey
- OptionText
- NextSceneIndex

## Notes
The next scene index is essential for path resolution.

---

# 5.6 SimulationSession

## Role
Represents a user’s active or completed execution of a script.

## Responsibilities
- reference the selected script
- store current execution status
- own the user’s decision vote(s)
- expose the resolved branch path

## Suggested Fields
- SessionId
- ScriptId
- Status
- BranchPathHash
- CreatedAt

## Suggested V1 Status values
- Created
- InProgress
- DecisionCompleted
- RenderRequested
- RenderCompleted
- Failed

## Key Behaviors
- record vote
- build branch path
- calculate branch path hash
- transition status

---

# 5.7 DecisionVote

## Role
Represents the selected option for a decision node within one session.

## Responsibilities
- preserve the exact decision made
- make branch path reconstruction possible

## Suggested Fields
- DecisionVoteId
- SessionId
- DecisionNodeId
- OptionKey
- CastAt

## Notes
V1 is single-user, so this entity does not yet require ParticipantId.

---

# 5.8 BranchPath (Value Object)

## Role
Represents the deterministic path of selected options.

## Example
`D1:A`

## Responsibilities
- provide deterministic narrative selection
- support stable hashing
- support render reproducibility

## Notes
This should be a value object, not a full entity.

## Suggested Members
- RawValue
- HashValue

## Key Behaviors
- build from ordered votes
- compute hash
- compare equality by value

---

# 5.9 RenderJob

## Role
Represents one request to generate final video from a session’s selected path.

## Responsibilities
- own render clips
- track render lifecycle
- store selected provider model
- store estimated/actual execution facts

## Suggested Fields
- RenderJobId
- SessionId
- BranchPathHash
- Status
- DurationSeconds
- Model
- FinalVideoUrl
- CreatedAt
- CompletedAt

## Suggested V1 Status values
- Queued
- Running
- Assembling
- Succeeded
- Failed

## Key Behaviors
- add clip
- mark running
- mark assembling
- mark succeeded
- mark failed

---

# 5.10 RenderClip

## Role
Represents one planned and executed clip in the render job.

## Responsibilities
- preserve clip order
- preserve scene association
- preserve prompt seed
- preserve provider task reference
- preserve output URL and status

## Suggested Fields
- RenderClipId
- RenderJobId
- ClipIndex
- SceneIndex
- BeatType
- PromptSeed
- PlannedDurationSeconds
- ProviderTaskId
- OutputBlobUrl
- Status

## Suggested V1 Status values
- Planned
- Submitted
- Running
- Succeeded
- Failed

## Key Behaviors
- assign provider task
- mark submitted
- mark succeeded
- mark failed

---

# 6. Ownership of Clip Planning

This must be explicit.

## Domain Layer Responsibility
The domain owns:
- script structure
- scene ordering
- decision semantics
- branch meaning

## Application Layer Responsibility
The application owns:
- branch resolution
- beat expansion
- clip planning
- cost estimation
- render plan construction

## Infrastructure Layer Responsibility
Infrastructure owns:
- provider communication
- file storage
- polling
- binary download

## Important Rule
`RenderClip` is a domain/application concept.  
It is **not** a Runway concept.

That separation is essential.

---

# 7. Domain Invariants

## Script Aggregate Invariants
- scene indexes must be unique inside a script
- decision options must point to valid scene indexes
- decision nodes must belong to exactly one script

## Session Aggregate Invariants
- session must reference one script
- branch path must be built only from valid decision votes
- in V1, only one vote may exist per decision node

## Render Aggregate Invariants
- render job must reference exactly one session
- render clips must have unique clip indexes within a render job
- final video URL may only exist when render job is succeeded

---

# 8. Lifecycle Alignment

## Script Lifecycle
Created → Persisted → Used by Session

## Session Lifecycle
Created → InProgress → DecisionCompleted → RenderRequested → RenderCompleted

## RenderJob Lifecycle
Queued → Running → Assembling → Succeeded / Failed

## RenderClip Lifecycle
Planned → Submitted → Running → Succeeded / Failed

These lifecycles must be reflected in the implementation.

---

# 9. Relationship Map

## One SimulationTemplate
can produce many Scripts

## One Script
has many ScriptScenes  
has many DecisionNodes

## One DecisionNode
has many DecisionOptions

## One Script
can have many SimulationSessions

## One SimulationSession
has many DecisionVotes  
can produce one RenderJob in V1

## One RenderJob
has many RenderClips

---

# 10. What Changes Compared to Earlier High-Level Model

After clip planning, we now know the following must be explicit:

## New or Strengthened Concepts
- SceneType
- BranchPath as value object
- RenderClip as planned unit, not only execution artifact
- BeatType captured at clip level
- PlannedDurationSeconds captured before provider submission
- FinalVideoUrl belongs to RenderJob

These are not optional refinements. They are required for correctness.

---

# 11. Recommended Folder Structure in Domain Project

```text
ScenarioCore.Domain
│
├── Templates
│   └── SimulationTemplate.cs
│
├── Scripts
│   ├── Script.cs
│   ├── ScriptScene.cs
│   ├── DecisionNode.cs
│   └── DecisionOption.cs
│
├── Sessions
│   ├── SimulationSession.cs
│   ├── DecisionVote.cs
│   └── BranchPath.cs
│
├── Rendering
│   ├── RenderJob.cs
│   └── RenderClip.cs
│
└── Common
    ├── Enums
    └── ValueObjects
```

---

# 12. Suggested Enums

## SceneType
- Introduction
- Context
- Suspense
- Decision
- Outcome
- Ending

## SessionStatus
- Created
- InProgress
- DecisionCompleted
- RenderRequested
- RenderCompleted
- Failed

## RenderJobStatus
- Queued
- Running
- Assembling
- Succeeded
- Failed

## RenderClipStatus
- Planned
- Submitted
- Running
- Succeeded
- Failed

## BeatType
- Setup
- Context
- Tension
- DecisionBuild
- Outcome
- Transition
- Closure

---

# 13. Immediate Consequence for Persistence

Because the clip planner emits structured render clips, the persistence model must support:

- planned clip records before provider submission
- prompt seed storage
- planned duration storage
- provider task id assignment later
- output URL assignment later

This means RenderClips must be persisted as part of render plan creation, not created only after Runway submission.

That is an important architectural correction.

---

# 14. Immediate Consequence for API Design

The API does not need to expose clip planning directly to the user, but the backend must support:

- creating render jobs from sessions
- inspecting render status
- later retrieving final video URL

Clip planning remains internal.

---

# 15. Exit Criteria

This refinement is complete for V1 when:

- every core entity has clear responsibility
- aggregate boundaries are stable
- branch path is explicitly modeled
- render clips are explicitly modeled as planned units
- domain/application/infrastructure boundaries are unambiguous
- the next persistence mapping step can proceed without guessing

---

# 16. Immediate Next Step in Execution Plan

After this domain refinement, the correct next step is:

**Persistence Design and EF Core Mapping Strategy**

At that stage, we will translate this domain into:
- SQL Server tables
- EF Core entity mappings
- DbContext boundaries
- migration order
