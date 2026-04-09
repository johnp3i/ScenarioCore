# ScenarioCore — Persistence Design & EF Core Mapping Strategy (V1)

## Purpose

This document defines the persistence design and EF Core mapping strategy for ScenarioCore V1.

It translates the refined domain model into a practical SQL Server + EF Core implementation plan while preserving:

- aggregate boundaries
- deterministic branch execution
- clip planning correctness
- render orchestration integrity
- future extensibility

This document is part of the ordered V1 execution plan and comes after:

1. execution plan
2. clip planning algorithm
3. domain model refinement aligned to clip planning

---

# 1. Persistence Design Goals

The persistence layer for V1 must achieve the following:

- persist the complete end-to-end MVP flow
- preserve narrative ordering
- support deterministic branch reconstruction
- support planned render clips before provider submission
- support background worker updates during rendering
- remain minimal enough for V1 but structurally extensible

It must **not**:

- leak infrastructure/provider concerns into the domain
- over-model future enterprise features
- optimize prematurely for multi-tenant complexity
- force redesign when moving from single clip to full clip orchestration

---

# 2. Persistence Scope for V1

## Included
- SimulationTemplate
- Script
- ScriptScene
- DecisionNode
- DecisionOption
- SimulationSession
- DecisionVote
- RenderJob
- RenderClip

## Excluded for V1
- subscription/billing
- analytics
- participants / multi-user collaboration
- clip cache reuse tables
- audit event store
- prompt version history
- advanced provider metrics tables

These excluded concerns can be added later without breaking the V1 core.

---

# 3. DbContext Strategy

## Recommendation: Single DbContext for V1

Use one DbContext:

`ScenarioCoreDbContext`

### Why
For V1, a single DbContext is the correct trade-off because:
- the domain is still small
- transactions are simpler
- migrations remain easier to manage
- orchestration logic is easier to debug
- development speed is higher

### Future Direction
If the system expands significantly, bounded contexts may later move to:
- CatalogDbContext
- SessionDbContext
- RenderingDbContext

But that is **not** recommended for V1.

---

# 4. Aggregate Persistence Boundaries

The persistence design must align with aggregate ownership.

## 4.1 Script Aggregate
Root:
- Script

Owned entities / related entities:
- ScriptScene
- DecisionNode
- DecisionOption

## 4.2 Session Aggregate
Root:
- SimulationSession

Owned / related:
- DecisionVote

## 4.3 Render Aggregate
Root:
- RenderJob

Owned / related:
- RenderClip

## 4.4 Template Catalog
Root:
- SimulationTemplate

This may be seeded and treated like reference data.

---

# 5. Table Design Strategy

## General Principles
- Use `UNIQUEIDENTIFIER` primary keys
- Use `DATETIME2` for timestamps
- Prefer explicit FK constraints
- Use explicit max lengths where practical
- Use NVARCHAR(MAX) only where narrative content is variable
- Preserve indexes on ordered and lookup columns

---

# 6. Entity-to-Table Mapping Overview

| Domain Entity | Table |
|---|---|
| SimulationTemplate | SimulationTemplates |
| Script | Scripts |
| ScriptScene | ScriptScenes |
| DecisionNode | DecisionNodes |
| DecisionOption | DecisionOptions |
| SimulationSession | SimulationSessions |
| DecisionVote | DecisionVotes |
| RenderJob | RenderJobs |
| RenderClip | RenderClips |

---

# 7. BranchPath Persistence Strategy

## Domain View
`BranchPath` is a value object.

## Persistence View
For V1, persist only:
- `BranchPathHash` on `SimulationSessions`
- `BranchPathHash` on `RenderJobs`

### Why
The branch path is reconstructable from `DecisionVotes`, so we do not need a dedicated table for it in V1.

### Recommendation
Persist:
- `BranchPathHash` as `NVARCHAR(200)` or `CHAR(64)` if using SHA256 hex

Preferred V1 column:
`CHAR(64)`

### Note
If you later want full explicit branch path persistence:
- add `BranchPathRaw` column
- or add `SessionBranchPaths` table

But V1 does not require it.

---

# 8. Enum Mapping Strategy

Enums should remain enums in the domain but be persisted as strings in SQL Server for readability.

## Recommended Enums
- `SceneType`
- `SessionStatus`
- `RenderJobStatus`
- `RenderClipStatus`
- `BeatType`

## EF Core Rule
Use `.HasConversion<string>()`

### Why
- improves DB readability
- safer for versioning than integer enums
- easier debugging
- easier partner/internal inspection

---

# 9. JSON Persistence Strategy

The following fields may reasonably remain JSON strings in V1:

- `AllowedDurationsJson` on `SimulationTemplates`
- `DialogueJson` on `ScriptScenes`

### Why
V1 does not yet need relational decomposition of dialogue structure.

### Rule
Store as `NVARCHAR(MAX)`.

### Future Note
If dialogue querying becomes important:
- normalize dialogue into separate tables later

---

# 10. Scene Ordering Strategy

`SceneIndex` is critical for deterministic execution.

## Rules
- `SceneIndex` must be unique per `Script`
- must be indexed
- must never be nullable

### EF Mapping
Use unique index:
- `(ScriptId, SceneIndex)`

---

# 11. Decision Option Resolution Strategy

`DecisionOption.NextSceneIndex` is the core narrative branch pointer.

## Rule
- required
- validated before persistence completion
- no dangling references allowed

### Note
SQL cannot easily FK this to `ScriptScenes.SceneIndex` because `SceneIndex` is scoped by script, not globally unique.

So:
- enforce with application/domain validation
- not with direct FK to scene index

This is intentional and correct.

---

# 12. RenderClip Persistence Strategy

This is one of the most important design choices.

## Key Principle
`RenderClip` must exist **before** provider submission.

Why:
- clip planning creates planned units
- workers need records to claim/update
- statuses must evolve over time
- provider task ids are assigned later

### Therefore RenderClips must store:
- planned ordering
- planned duration
- scene association
- beat type
- prompt seed
- provider task id (nullable initially)
- output URL (nullable initially)
- status

This is essential to support the real execution flow.

---

# 13. SQL Table Definitions (Recommended V1)

## 13.1 SimulationTemplates
Suggested columns:
- SimulationTemplateId UNIQUEIDENTIFIER PK
- Code NVARCHAR(100) NOT NULL UNIQUE
- Title NVARCHAR(200) NOT NULL
- Description NVARCHAR(MAX) NULL
- MaxCharacters INT NOT NULL
- AllowedDurationsJson NVARCHAR(MAX) NOT NULL
- Version INT NOT NULL
- Status NVARCHAR(50) NOT NULL

---

## 13.2 Scripts
Suggested columns:
- ScriptId UNIQUEIDENTIFIER PK
- SimulationTemplateId UNIQUEIDENTIFIER NOT NULL
- DurationMinutes INT NOT NULL
- CharacterCount INT NOT NULL
- CreatedAt DATETIME2 NOT NULL

FK:
- Scripts → SimulationTemplates

---

## 13.3 ScriptScenes
Suggested columns:
- ScriptSceneId UNIQUEIDENTIFIER PK
- ScriptId UNIQUEIDENTIFIER NOT NULL
- SceneIndex INT NOT NULL
- SceneDescription NVARCHAR(MAX) NULL
- DialogueJson NVARCHAR(MAX) NULL
- SceneType NVARCHAR(50) NOT NULL

FK:
- ScriptScenes → Scripts

Unique Index:
- (ScriptId, SceneIndex)

---

## 13.4 DecisionNodes
Suggested columns:
- DecisionNodeId UNIQUEIDENTIFIER PK
- ScriptId UNIQUEIDENTIFIER NOT NULL
- SceneIndex INT NOT NULL
- Prompt NVARCHAR(MAX) NOT NULL

FK:
- DecisionNodes → Scripts

Index:
- (ScriptId, SceneIndex)

---

## 13.5 DecisionOptions
Suggested columns:
- DecisionOptionId UNIQUEIDENTIFIER PK
- DecisionNodeId UNIQUEIDENTIFIER NOT NULL
- OptionKey CHAR(1) NOT NULL
- OptionText NVARCHAR(MAX) NOT NULL
- NextSceneIndex INT NOT NULL

FK:
- DecisionOptions → DecisionNodes

Unique Index:
- (DecisionNodeId, OptionKey)

---

## 13.6 SimulationSessions
Suggested columns:
- SessionId UNIQUEIDENTIFIER PK
- ScriptId UNIQUEIDENTIFIER NOT NULL
- Status NVARCHAR(50) NOT NULL
- BranchPathHash CHAR(64) NULL
- CreatedAt DATETIME2 NOT NULL

FK:
- SimulationSessions → Scripts

Index:
- (ScriptId)
- (Status)

---

## 13.7 DecisionVotes
Suggested columns:
- DecisionVoteId UNIQUEIDENTIFIER PK
- SessionId UNIQUEIDENTIFIER NOT NULL
- DecisionNodeId UNIQUEIDENTIFIER NOT NULL
- OptionKey CHAR(1) NOT NULL
- CastAt DATETIME2 NOT NULL

FK:
- DecisionVotes → SimulationSessions
- DecisionVotes → DecisionNodes

Unique Index:
- (SessionId, DecisionNodeId)

This enforces only one vote per node per session in V1.

---

## 13.8 RenderJobs
Suggested columns:
- RenderJobId UNIQUEIDENTIFIER PK
- SessionId UNIQUEIDENTIFIER NOT NULL
- BranchPathHash CHAR(64) NOT NULL
- Status NVARCHAR(50) NOT NULL
- DurationSeconds INT NOT NULL
- Model NVARCHAR(50) NOT NULL
- FinalVideoUrl NVARCHAR(500) NULL
- CreatedAt DATETIME2 NOT NULL
- CompletedAt DATETIME2 NULL

FK:
- RenderJobs → SimulationSessions

Index:
- (SessionId)
- (Status)

---

## 13.9 RenderClips
Suggested columns:
- RenderClipId UNIQUEIDENTIFIER PK
- RenderJobId UNIQUEIDENTIFIER NOT NULL
- ClipIndex INT NOT NULL
- SceneIndex INT NOT NULL
- BeatType NVARCHAR(50) NOT NULL
- PromptSeed NVARCHAR(MAX) NOT NULL
- PlannedDurationSeconds INT NOT NULL
- ProviderTaskId NVARCHAR(200) NULL
- OutputBlobUrl NVARCHAR(500) NULL
- Status NVARCHAR(50) NOT NULL

FK:
- RenderClips → RenderJobs

Unique Index:
- (RenderJobId, ClipIndex)

Index:
- (RenderJobId, Status)

---

# 14. Recommended EF Core Entity Configurations

Use `IEntityTypeConfiguration<T>` for all mappings.

## Why
- keeps DbContext clean
- separates configuration from entity classes
- easier for KIRO agents to manage
- easier to extend and test

### Suggested folder
```text
ScenarioCore.Infrastructure
└── Persistence
    └── Configurations
```

---

# 15. Suggested Mapping Rules Per Entity

## 15.1 SimulationTemplateConfiguration
- table: `SimulationTemplates`
- key: `SimulationTemplateId`
- unique index on `Code`
- `Code` max length 100
- `Title` max length 200
- `Status` as string enum conversion

## 15.2 ScriptConfiguration
- table: `Scripts`
- key: `ScriptId`
- required FK to `SimulationTemplate`
- no cascade delete from template in V1 unless intentional

## 15.3 ScriptSceneConfiguration
- table: `ScriptScenes`
- key: `ScriptSceneId`
- required FK to `Script`
- unique index on `(ScriptId, SceneIndex)`
- `SceneType` as string enum conversion

## 15.4 DecisionNodeConfiguration
- table: `DecisionNodes`
- key: `DecisionNodeId`
- required FK to `Script`
- index on `(ScriptId, SceneIndex)`

## 15.5 DecisionOptionConfiguration
- table: `DecisionOptions`
- key: `DecisionOptionId`
- required FK to `DecisionNode`
- unique index on `(DecisionNodeId, OptionKey)`

## 15.6 SimulationSessionConfiguration
- table: `SimulationSessions`
- key: `SessionId`
- required FK to `Script`
- `Status` as string enum conversion
- BranchPathHash fixed length 64 if possible

## 15.7 DecisionVoteConfiguration
- table: `DecisionVotes`
- key: `DecisionVoteId`
- required FK to `SimulationSession`
- required FK to `DecisionNode`
- unique index on `(SessionId, DecisionNodeId)`

## 15.8 RenderJobConfiguration
- table: `RenderJobs`
- key: `RenderJobId`
- required FK to `SimulationSession`
- `Status` as string enum conversion
- `FinalVideoUrl` nullable

## 15.9 RenderClipConfiguration
- table: `RenderClips`
- key: `RenderClipId`
- required FK to `RenderJob`
- unique index on `(RenderJobId, ClipIndex)`
- `BeatType` as string enum conversion
- `Status` as string enum conversion

---

# 16. DbContext Structure

## Recommended DbSet List

```csharp
DbSet<SimulationTemplate> SimulationTemplates
DbSet<Script> Scripts
DbSet<ScriptScene> ScriptScenes
DbSet<DecisionNode> DecisionNodes
DbSet<DecisionOption> DecisionOptions
DbSet<SimulationSession> SimulationSessions
DbSet<DecisionVote> DecisionVotes
DbSet<RenderJob> RenderJobs
DbSet<RenderClip> RenderClips
```

## Recommended OnModelCreating
- apply all configurations from assembly
- keep no inline configuration logic unless temporary

Example approach:
`modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScenarioCoreDbContext).Assembly);`

---

# 17. Cascade Delete Strategy

Use cascade delete carefully.

## Recommended V1 Behavior

### Script → ScriptScenes
Cascade allowed

### Script → DecisionNodes
Cascade allowed

### DecisionNode → DecisionOptions
Cascade allowed

### Script → SimulationSessions
Do **not** cascade automatically in V1

### SimulationSession → RenderJobs
Do **not** cascade automatically in V1

### RenderJob → RenderClips
Cascade allowed

### Why
Operational data (sessions, renders) should not disappear automatically because a script record is changed or removed.

That data is closer to runtime history than reference configuration.

---

# 18. Concurrency Strategy

V1 can remain simple.

## Recommendation
No rowversion columns required for all tables yet.

### Exception
If worker contention becomes relevant early, later consider concurrency tokens on:
- RenderJobs
- RenderClips

But for V1:
- status updates can be controlled at application level
- no need to over-design upfront

---

# 19. Query Patterns to Support

Persistence must support these core queries efficiently.

## Script Flow
- get template by code
- load script with scenes + decision nodes + options

## Session Flow
- load session by id
- load votes for session
- compute branch path

## Render Flow
- get render job by id
- get clips by render job ordered by ClipIndex
- get clips by status for worker processing

Indexes recommended above support these patterns.

---

# 20. Migration Strategy

## Initial Migration Must Include
- all 9 V1 tables
- indexes
- constraints
- seed data for at least one SimulationTemplate

## Migration Naming Convention
Use explicit names, e.g.:
- `InitialScenarioCoreV1`
- `SeedInitialTemplates`

## Recommendation
Seed reference data through EF configuration or startup initializer, not manual SQL unless required.

---

# 21. KIRO Agent Guidance

KIRO agents should follow these rules:

- create one configuration class per entity
- do not place mapping rules inside entities
- preserve aggregate ownership in navigation properties
- avoid accidental many-to-many modeling
- use string enum conversions
- create indexes explicitly
- do not create branch path table in V1
- persist RenderClips before provider submission
- keep provider-specific state out of domain classes

---

# 22. Suggested Project/Folder Layout for Persistence

```text
ScenarioCore.Infrastructure
│
├── Persistence
│   ├── ScenarioCoreDbContext.cs
│   ├── Configurations
│   │   ├── SimulationTemplateConfiguration.cs
│   │   ├── ScriptConfiguration.cs
│   │   ├── ScriptSceneConfiguration.cs
│   │   ├── DecisionNodeConfiguration.cs
│   │   ├── DecisionOptionConfiguration.cs
│   │   ├── SimulationSessionConfiguration.cs
│   │   ├── DecisionVoteConfiguration.cs
│   │   ├── RenderJobConfiguration.cs
│   │   └── RenderClipConfiguration.cs
│   └── Migrations
```

---

# 23. Immediate Consequences for Next Implementation Phase

After this persistence strategy, implementation can proceed in a stable way to:

1. create the real solution/project structure
2. implement the entities and enums
3. create EF Core mappings
4. create the initial migration
5. seed the first simulation template

At that point, the persistence foundation for ScenarioCore V1 will be correctly established.

---

# 24. Exit Criteria

This persistence design is complete for V1 when:

- every core V1 domain entity has a clear table mapping
- aggregate boundaries are respected
- branch path persistence is defined
- enum persistence is defined
- render clip planning persistence is defined
- DbContext strategy is defined
- migration strategy is defined
- implementation can begin without guessing

---

# 25. Immediate Next Step in Execution Plan

After this document, the correct next step is:

**Create the actual ScenarioCore solution and project structure, then implement the entities + EF Core configurations.**
