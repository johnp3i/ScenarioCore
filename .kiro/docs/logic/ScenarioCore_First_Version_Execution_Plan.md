# ScenarioCore — First Version Execution Plan

## Purpose

This document defines the **ordered execution plan** for building the **first version (V1)** of ScenarioCore.

It exists to ensure:

- consistency in implementation order
- architectural discipline
- clear phase boundaries
- reduced technical risk
- KIRO-agent comprehensiveness

This plan is intentionally sequential.  
It prevents jumping between infrastructure, domain, rendering, and UI without first validating the dependencies of each stage.

---

# 1. Project Description

## 1.1 What ScenarioCore Is

ScenarioCore is an **enterprise-first decision simulation platform**.

It enables organizations to:

- generate structured simulation scripts
- introduce controlled decision points
- allow users to choose actions
- render the resulting path as short AI-generated video
- use those simulations for training and behavioral observation

ScenarioCore is **not**:

- a film creation platform
- a generic AI story generator
- a user-authored screenplay system
- a psychometric or IQ assessment product

ScenarioCore V1 is focused on:

- predefined simulation templates
- short single-user proof-of-concept sessions
- one decision node
- clip-based AI rendering through Runway
- a minimal but correct technical foundation in .NET

---

## 1.2 V1 Objective

Deliver a working end-to-end system:

**Template → Script → Session → Decision → BranchPath → Clip Plan → Render → Final Video**

V1 scope:

- one simulation template
- one generated script structure
- one decision node
- one user session
- one render job
- clip-based video generation
- final stitched MP4

This first version is intended to prove:

- technical feasibility
- orchestration correctness
- provider integration viability
- cost realism
- foundation quality for expansion

---

# 2. Guiding Principles

## 2.1 Sequential Execution

We will build the system in the following order:

1. execution plan
2. clip planning algorithm
3. domain model refinement
4. database schema and persistence mapping
5. provider abstractions
6. script generation flow
7. session + decision flow
8. render orchestration
9. worker execution
10. user platform
11. stabilization and MVP validation

This order is deliberate.

---

## 2.2 Minimal First Version

V1 must remain minimal:

- no multi-user voting
- no analytics dashboard
- no advanced subscription billing
- no behavioral scoring
- no mobile application
- no full enterprise reporting

---

## 2.3 Provider Isolation

External AI providers must remain behind abstractions:

- Script AI provider
- Video AI provider
- Blob storage provider

No domain logic may depend directly on provider SDK behavior.

---

## 2.4 Deterministic Execution

The platform must be deterministic where possible:

- scripts must have predictable structure
- branch paths must be reproducible
- clip plans must be derived from known rules
- render jobs must be auditable

---

# 3. Ordered Development Phases

# Phase 0 — Planning Baseline

## Goal
Establish the execution order, scope boundaries, and project foundations before implementation expands.

## Outputs
- execution plan
- MVP roadmap
- architecture diagram
- sequence diagram
- database baseline
- sprint tickets

## Status
In progress / largely completed.

---

# Phase 1 — Clip Planning Algorithm

## Why This Comes First
The clip planning algorithm is central to the rendering engine.

It determines:

- how scripts are translated into clips
- how many Runway requests are needed
- how render costs scale
- how branch paths affect rendering
- how final assembly works

Without this algorithm, the render pipeline is structurally incomplete.

## Goal
Define exactly how a selected branch path becomes an ordered list of renderable clips.

## Inputs
- script
- scenes
- decision node(s)
- chosen option(s)
- target duration
- clip duration strategy

## Outputs
- ordered clip plan
- clip indexes
- scene-to-clip mapping
- prompt seeds for each clip

## Deliverables
- clip planning algorithm specification
- examples for 60–90 second MVP
- branch-aware clip planning rules
- cost impact notes

## Exit Criteria
- we can determine the exact number of clips for a session
- we can map branch outcomes to clip sequences
- we can estimate render cost before submitting provider tasks

---

# Phase 2 — Domain Model Refinement

## Why It Comes After Clip Planning
Once the clip planning rules are defined, the render domain can be modeled correctly.

Otherwise, we risk creating entities that do not match the real orchestration needs.

## Goal
Refine the core V1 entities and aggregates around the actual execution flow.

## Core Entities
- Script
- ScriptScene
- DecisionNode
- DecisionOption
- SimulationSession
- DecisionVote
- RenderJob
- RenderClip

## Deliverables
- entity definitions
- aggregate boundaries
- invariants
- lifecycle rules
- branch path definition

## Exit Criteria
- domain entities align with clip planning and render orchestration
- no ambiguous ownership of responsibilities

---

# Phase 3 — Persistence Design

## Goal
Convert the refined domain into a practical SQL Server persistence model.

## Scope
- minimal V1 database tables
- relationships
- indexes
- EF Core mapping strategy
- DbContext boundaries

## Deliverables
- SQL schema
- entity-to-table mapping rules
- DbContext definition plan
- migration order

## Exit Criteria
- persistence model supports the clip planning and render flow
- schema is stable enough for implementation

---

# Phase 4 — Solution and Project Structure

## Goal
Create the actual .NET solution layout that will host the V1 implementation.

## Projects
- ConsolePoC
- ScenarioCore.Api
- ScenarioCore.Platform
- ScenarioCore.Domain
- ScenarioCore.Application
- ScenarioCore.Infrastructure
- ScenarioCore.Workers
- ScenarioCore.Contracts
- ScenarioCore.Tests

## Deliverables
- solution structure
- project references
- dependency direction
- development conventions

## Exit Criteria
- solution compiles
- dependency graph is clean
- no circular references

---

# Phase 5 — Infrastructure Foundations

## Goal
Set up the technical platform foundations required before business flows can run.

## Scope
- EF Core
- SQL Server connectivity
- configuration
- logging
- blob storage abstraction
- background worker host
- authentication skeleton
- outbox groundwork if needed

## Deliverables
- bootstrapped solution
- persistence connection
- storage abstraction
- worker startup capability

## Exit Criteria
- infrastructure can support application flows
- no manual hacks are required for provider calls later

---

# Phase 6 — Video Provider Integration

## Why Before Full Script Flow
This is the highest-risk technical dependency and has already been proven at PoC level.

Now it must be formalized as infrastructure.

## Goal
Convert the proven ConsolePoC logic into reusable provider infrastructure.

## Scope
- IVideoProviderClient
- RunwayVideoProviderClient
- task creation
- polling
- output URL retrieval
- binary download logic

## Deliverables
- provider interface
- Runway implementation
- error handling rules
- timeout rules
- polling rules
- result models

## Exit Criteria
- one generated clip can be requested via the infrastructure layer
- provider is reusable by workers

---

# Phase 7 — Script Provider Integration

## Goal
Create the infrastructure required to generate structured scripts from templates.

## Scope
- IScriptProviderClient
- script request contract
- AI response normalization
- deterministic JSON structure

## Deliverables
- script provider contract
- provider implementation
- validation of script structure

## Exit Criteria
- system can request and persist one valid structured script

---

# Phase 8 — Script Flow Implementation

## Goal
Implement the first business flow: create script from predefined template.

## Scope
- template selection
- script generation
- scene persistence
- decision node persistence

## Deliverables
- GenerateScript use case
- POST /api/scripts
- database persistence
- retrieval model

## Exit Criteria
- one script can be created and stored correctly

---

# Phase 9 — Session and Decision Flow

## Goal
Implement the second business flow: create a session and capture a decision.

## Scope
- create session
- attach script
- submit decision
- compute branch path
- persist branch path hash

## Deliverables
- CreateSession use case
- SubmitDecision use case
- session endpoints
- branch path builder

## Exit Criteria
- a script can become an executable session
- one decision produces one deterministic branch path

---

# Phase 10 — Clip Planning Execution

## Goal
Apply the clip planning algorithm to a real session.

## Scope
- derive clip plan from branch path
- produce RenderClip records
- estimate provider cost
- assign prompts per clip

## Deliverables
- clip planner implementation
- render plan builder
- prompt expansion strategy

## Exit Criteria
- a chosen decision path can become a real ordered render plan

---

# Phase 11 — Render Orchestration

## Goal
Create a render job from the clip plan and prepare it for worker execution.

## Scope
- create RenderJob
- create RenderClips
- status transitions
- render request endpoint

## Deliverables
- RequestRender use case
- POST /api/renders
- render job persistence

## Exit Criteria
- system can create a render job from a session

---

# Phase 12 — Worker Execution and Final Assembly

## Goal
Execute actual clip rendering in the background and assemble final output.

## Scope
- background worker
- submit clips to Runway
- poll tasks
- store outputs
- stitch clips with FFmpeg
- finalize MP4

## Deliverables
- RenderWorker
- polling orchestration
- assembly process
- output persistence

## Exit Criteria
- one session can produce one final downloadable MP4

---

# Phase 13 — User Platform (MVC)

## Why After Core Flows
The user platform should sit on top of stable API flows, not define them.

## Goal
Create the first user-facing web platform for interacting with ScenarioCore.

## Scope
- login placeholder or simple auth
- select template
- create script
- create session
- submit decision
- request render
- view status
- play final video

## Deliverables
- ScenarioCore.Platform
- MVC views
- API client integration
- minimal dashboard

## Exit Criteria
- a user can complete the entire MVP flow from the browser

---

# Phase 14 — MVP Stabilization

## Goal
Validate that V1 is stable enough for internal demo and partner discussion.

## Scope
- bug fixing
- logging review
- retry hardening
- edge case cleanup
- cost observation
- timing measurement

## Deliverables
- stable demo flow
- known issues list
- V1 limitations list
- next-phase recommendations

## Exit Criteria
- internal team can run the full flow repeatedly
- MVP can be demonstrated with confidence

---

# 4. Summary of Exact Build Order

## Ordered List

1. finalize execution plan
2. define clip planning algorithm
3. refine domain model around clip planning
4. finalize persistence model
5. create solution + project structure
6. implement infrastructure foundations
7. implement Runway provider client
8. implement script provider client
9. implement script generation flow
10. implement session + decision flow
11. implement clip planning service
12. implement render job creation
13. implement worker render execution
14. implement FFmpeg assembly
15. implement user platform
16. stabilize and validate MVP

This is the official V1 execution order.

---

# 5. V1 Scope Boundaries

## Included
- one template
- one decision node
- one user
- one branch path
- clip-based rendering
- Runway integration
- final MP4 assembly
- MVC user platform
- SQL Server persistence
- .NET 8 solution structure

## Excluded
- multi-user collaboration
- full subscription engine
- token billing UI
- advanced analytics
- psychometric scoring
- multi-template marketplace
- enterprise administration portal
- advanced branching graphs

---

# 6. Success Definition for First Version

ScenarioCore V1 is successful when:

- a user can choose a predefined scenario
- the system generates a structured script
- the user makes a decision
- the system computes a branch path
- the system creates a clip plan
- the worker renders the clips through Runway
- the system assembles a final MP4
- the user can watch the result in the platform

That is the first true proof of ScenarioCore.

---

# 7. Immediate Next Step

The next implementation artifact to create is:

**Clip Planning Algorithm Specification**

That is the next correct step in the execution order.
