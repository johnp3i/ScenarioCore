# ScenarioCore — MVP Database Schema (SQL Server)

This file contains the complete minimal database schema for ScenarioCore MVP.

---

## Scripts

CREATE TABLE Scripts
(
    ScriptId UNIQUEIDENTIFIER PRIMARY KEY,
    TemplateCode NVARCHAR(100) NOT NULL,
    DurationMinutes INT NOT NULL,
    CharacterCount INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);

---

## ScriptScenes

CREATE TABLE ScriptScenes
(
    ScriptSceneId UNIQUEIDENTIFIER PRIMARY KEY,
    ScriptId UNIQUEIDENTIFIER NOT NULL,
    SceneIndex INT NOT NULL,
    SceneDescription NVARCHAR(MAX),
    DialogueJson NVARCHAR(MAX),

    CONSTRAINT FK_ScriptScenes_Scripts
    FOREIGN KEY (ScriptId)
    REFERENCES Scripts(ScriptId)
);

---

## DecisionNodes

CREATE TABLE DecisionNodes
(
    DecisionNodeId UNIQUEIDENTIFIER PRIMARY KEY,
    ScriptId UNIQUEIDENTIFIER NOT NULL,
    SceneIndex INT NOT NULL,
    Prompt NVARCHAR(MAX),

    CONSTRAINT FK_DecisionNodes_Scripts
    FOREIGN KEY (ScriptId)
    REFERENCES Scripts(ScriptId)
);

---

## DecisionOptions

CREATE TABLE DecisionOptions
(
    DecisionOptionId UNIQUEIDENTIFIER PRIMARY KEY,
    DecisionNodeId UNIQUEIDENTIFIER NOT NULL,
    OptionKey CHAR(1) NOT NULL,
    OptionText NVARCHAR(MAX),
    NextSceneIndex INT NOT NULL,

    CONSTRAINT FK_DecisionOptions_DecisionNodes
    FOREIGN KEY (DecisionNodeId)
    REFERENCES DecisionNodes(DecisionNodeId)
);

---

## SimulationSessions

CREATE TABLE SimulationSessions
(
    SessionId UNIQUEIDENTIFIER PRIMARY KEY,
    ScriptId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    BranchPathHash NVARCHAR(200),
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT FK_Sessions_Scripts
    FOREIGN KEY (ScriptId)
    REFERENCES Scripts(ScriptId)
);

---

## DecisionVotes

CREATE TABLE DecisionVotes
(
    DecisionVoteId UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    DecisionNodeId UNIQUEIDENTIFIER NOT NULL,
    OptionKey CHAR(1) NOT NULL,
    CastAt DATETIME2 NOT NULL,

    CONSTRAINT FK_Votes_Session
    FOREIGN KEY (SessionId)
    REFERENCES SimulationSessions(SessionId)
);

---

## RenderJobs

CREATE TABLE RenderJobs
(
    RenderJobId UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    BranchPathHash NVARCHAR(200),
    Status NVARCHAR(50),
    DurationSeconds INT,
    Model NVARCHAR(50),
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,

    CONSTRAINT FK_RenderJobs_Session
    FOREIGN KEY (SessionId)
    REFERENCES SimulationSessions(SessionId)
);

---

## RenderClips

CREATE TABLE RenderClips
(
    RenderClipId UNIQUEIDENTIFIER PRIMARY KEY,
    RenderJobId UNIQUEIDENTIFIER NOT NULL,
    ClipIndex INT NOT NULL,
    SceneIndex INT NOT NULL,
    PromptText NVARCHAR(MAX),
    ProviderTaskId NVARCHAR(200),
    OutputBlobUrl NVARCHAR(500),
    Status NVARCHAR(50),

    CONSTRAINT FK_RenderClips_RenderJobs
    FOREIGN KEY (RenderJobId)
    REFERENCES RenderJobs(RenderJobId)
);

---

## Notes

- This schema is intentionally minimal for MVP.
- Supports full pipeline: Script → Session → Decision → Render.
- Designed for extension (tokens, users, analytics later).
