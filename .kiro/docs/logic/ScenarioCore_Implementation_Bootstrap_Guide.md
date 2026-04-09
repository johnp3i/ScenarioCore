# ScenarioCore — Implementation Bootstrap Guide (KIRO-Ready)

## Purpose

This document provides the exact implementation bootstrap steps for ScenarioCore V1.

It is designed to be:

- deterministic
- executable step-by-step
- KIRO-agent friendly
- aligned with all prior architectural decisions

This is the first **implementation artifact**, transitioning from design → code.

---

# 1. Solution Creation

## Step 1 — Create Solution

```bash
dotnet new sln -n ScenarioCore
```

---

# 2. Create Projects

## Step 2 — Create All Projects

```bash
dotnet new webapi -n ScenarioCore.Api
dotnet new mvc -n ScenarioCore.Platform
dotnet new classlib -n ScenarioCore.Domain
dotnet new classlib -n ScenarioCore.Application
dotnet new classlib -n ScenarioCore.Infrastructure
dotnet new worker -n ScenarioCore.Workers
dotnet new classlib -n ScenarioCore.Contracts
dotnet new xunit -n ScenarioCore.Tests
```

---

# 3. Add Projects to Solution

```bash
dotnet sln add ScenarioCore.Api
dotnet sln add ScenarioCore.Platform
dotnet sln add ScenarioCore.Domain
dotnet sln add ScenarioCore.Application
dotnet sln add ScenarioCore.Infrastructure
dotnet sln add ScenarioCore.Workers
dotnet sln add ScenarioCore.Contracts
dotnet sln add ScenarioCore.Tests
```

---

# 4. Project References (CRITICAL)

## Apply EXACT dependency directions

```bash
dotnet add ScenarioCore.Api reference ScenarioCore.Application
dotnet add ScenarioCore.Api reference ScenarioCore.Infrastructure
dotnet add ScenarioCore.Api reference ScenarioCore.Contracts

dotnet add ScenarioCore.Platform reference ScenarioCore.Contracts

dotnet add ScenarioCore.Application reference ScenarioCore.Domain
dotnet add ScenarioCore.Application reference ScenarioCore.Contracts

dotnet add ScenarioCore.Infrastructure reference ScenarioCore.Application
dotnet add ScenarioCore.Infrastructure reference ScenarioCore.Domain
dotnet add ScenarioCore.Infrastructure reference ScenarioCore.Contracts

dotnet add ScenarioCore.Workers reference ScenarioCore.Application
dotnet add ScenarioCore.Workers reference ScenarioCore.Infrastructure
dotnet add ScenarioCore.Workers reference ScenarioCore.Contracts
```

---

# 5. Install Required NuGet Packages

```bash
dotnet add ScenarioCore.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add ScenarioCore.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add ScenarioCore.Infrastructure package Microsoft.EntityFrameworkCore.Design

dotnet add ScenarioCore.Api package Microsoft.EntityFrameworkCore.SqlServer
```

---

# 6. Domain Implementation

## Folder Structure

```text
ScenarioCore.Domain
│
├── Templates
├── Scripts
├── Sessions
├── Rendering
├── Common
│   ├── Enums
│   └── ValueObjects
```

---

## Enums

- SceneType
- SessionStatus
- RenderJobStatus
- RenderClipStatus
- BeatType

---

## Value Object

BranchPath:
- RawValue
- HashValue (SHA256)

---

## Entities

- SimulationTemplate
- Script
- ScriptScene
- DecisionNode
- DecisionOption
- SimulationSession
- DecisionVote
- RenderJob
- RenderClip

---

# 7. Infrastructure — Persistence

## Folder Structure

```text
ScenarioCore.Infrastructure
│
├── Persistence
│   ├── ScenarioCoreDbContext.cs
│   ├── Configurations
```

Create one configuration per entity.

---

# 8. DbContext

Include all DbSets and apply configurations from assembly.

---

# 9. Critical Rules

- Enum → string conversion
- Unique indexes enforced
- RenderClips persisted BEFORE provider call
- BranchPathHash CHAR(64)

---

# 10. Migration

```bash
dotnet ef migrations add InitialScenarioCoreV1 --project ScenarioCore.Infrastructure --startup-project ScenarioCore.Api
dotnet ef database update --project ScenarioCore.Infrastructure --startup-project ScenarioCore.Api
```

---

# 11. Seed Template

Create one template:

LeadershipConflict_v1

---

# 12. Exit Criteria

- solution builds
- migration runs
- schema correct
- indexes present
- ready for next phase

---

# 13. Next Step

Implement Clip Planning Service (Application Layer)
