# ScenarioCore — KIRO Agent Operating Guide

## Purpose

This document instructs a KIRO agent (or developer) **how to use the existing ScenarioCore specification files** to correctly implement the system.

This is a **navigation + execution manual**, not a design document.

---

# 1. Available Documents (Authoritative Sources)

The following files define the system:

1. ScenarioCore_First_Version_Execution_Plan.md  
2. ScenarioCore_Clip_Planning_Algorithm_Specification.md  
3. ScenarioCore_Domain_Model_Refinement_Aligned_to_Clip_Planning.md  
4. ScenarioCore_Persistence_Design_and_EF_Core_Mapping_Strategy.md  
5. ScenarioCore_Implementation_Bootstrap_Guide.md  
6. ScenarioCore_Clip_Planning_Service_Implementation.md  
7. ScenarioCore_Project_Progress_Status.md  

---

# 2. Golden Rule

KIRO must follow:

👉 **Order matters. Do NOT jump between documents randomly.**

Execution sequence is strictly:

1. Bootstrap
2. Domain
3. Persistence
4. Planner
5. Use Case

---

# 3. Step-by-Step Execution Guide

## STEP 1 — Bootstrap the Solution

Use:
➡️ ScenarioCore_Implementation_Bootstrap_Guide.md

KIRO must:
- create solution
- create projects
- set references EXACTLY
- install packages

❌ Do NOT implement logic yet

---

## STEP 2 — Implement Domain Layer

Use:
➡️ ScenarioCore_Domain_Model_Refinement_Aligned_to_Clip_Planning.md

KIRO must:
- create all entities
- create enums
- implement BranchPath value object
- follow aggregate boundaries

❌ No EF attributes
❌ No database logic

---

## STEP 3 — Implement Persistence Layer

Use:
➡️ ScenarioCore_Persistence_Design_and_EF_Core_Mapping_Strategy.md

KIRO must:
- create DbContext
- create configurations (Fluent API only)
- define indexes
- define constraints

CRITICAL:
- enums stored as string
- unique indexes implemented
- BranchPathHash fixed length

---

## STEP 4 — Create Database

Use:
➡️ Bootstrap Guide (Migration section)

KIRO must:
- create migration
- apply migration
- validate schema manually

---

## STEP 5 — Implement Clip Planning Algorithm

Use:
➡️ ScenarioCore_Clip_Planning_Algorithm_Specification.md  
➡️ ScenarioCore_Clip_Planning_Service_Implementation.md

KIRO must:
- implement IClipPlanner
- implement deterministic algorithm EXACTLY
- implement:
  - branch resolution
  - beat expansion
  - duration allocation
  - prompt generation
  - cost estimation

❌ No shortcuts  
❌ No AI logic  
❌ No randomness  

---

## STEP 6 — Implement Application Use Case

Use:
➡️ (Render Job document provided)

KIRO must:
- implement CreateRenderJobUseCase
- load data correctly
- reconstruct branch path
- call planner
- create RenderJob
- create RenderClips
- persist

CRITICAL:
RenderClips must be created BEFORE rendering.

---

## STEP 7 — Testing

KIRO must create:

### Unit Tests
- ClipPlanner
- Use case

### Integration Test
- persistence correctness

---

# 4. Document Responsibilities

## Execution Plan
Defines overall order and phases.

## Algorithm Spec
Defines HOW the planner works.

## Domain Model
Defines WHAT entities exist.

## Persistence Design
Defines HOW data is stored.

## Bootstrap Guide
Defines HOW to start the solution.

## Planner Implementation
Defines HOW to code the planner.

## Progress Status
Defines WHERE the project currently is.

---

# 5. Critical Constraints

KIRO must NEVER:

- mix Domain with EF Core
- call DB inside planner
- introduce randomness
- skip indexes
- change execution order
- introduce repositories prematurely

---

# 6. Definition of Done (MVP Backend)

The system is considered ready when:

- solution builds
- database created
- planner produces deterministic clips
- render job is created
- render clips persisted
- tests pass

---

# 7. Next Phase (After Completion)

After all above steps:

➡️ Implement Worker (Runway execution)

---

# 8. Final Instruction

KIRO must treat all documents as:

👉 **Authoritative specifications, not suggestions**

If any conflict appears:
➡️ Execution Plan has highest priority.

---

End of document
