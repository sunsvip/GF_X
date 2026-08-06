# Editor AI CLI Architecture

## Goal

Provide one shared AI CLI execution layer for Unity editor tools.

The shared layer is responsible only for running an AI task and converging task state.

Business modules are responsible for:

- preparing inputs
- building the final prompt
- defining result delivery files
- validating results
- applying results

## Layering

### Shared AI CLI Layer

Location:

- `Assets/AAAGame/ScriptsBuiltin/Editor/Core/AI/`

Responsibilities:

- resolve provider command name
- create task working directory
- write prompt file
- start CLI process
- pipe prompt into stdin
- capture stdout / stderr logs
- parse provider terminal events
- maintain shared task state and progress
- poll business result readiness
- call business validation and apply hooks

The shared layer must not contain business-specific JSON schema, file semantics, or apply logic.

### Business Task Layer

Examples:

- localization translation
- UI analysis / UI patch generation
- code generation
- editor asset processing helpers

Responsibilities:

- prepare business input files
- return the final prompt content
- define output file layout
- decide whether result files are ready
- validate output files
- apply output files back to project data

## Core Types

### `AiCliProvider`

Shared AI CLI provider enum:

- `CodexCli`
- `ClaudeCodeCli`
- `OpenCodeCli`

### `AiCliTaskStatusSnapshot`

Shared task status snapshot for UI:

- `Provider`
- `State`
- `Message`
- `Detail`
- `ErrorMessage`
- `Progress01`
- `CompletedUnits`
- `TotalUnits`
- `WorkingDirectory`
- `LastStdout`
- `LastStderr`

### `AiCliTaskContext`

Per-run execution context:

- task name
- provider
- working directory
- prompt path
- stdout / stderr paths
- output directory
- process handle
- transient terminal state
- business payload object

### `IAiCliTaskDefinition`

Business-defined AI task contract.

Required capabilities:

- `PrepareInputs`
- `BuildPrompt`
- `BuildRunningProgress`
- `TryFinalize`

The definition owns business rules. The executor owns process lifetime and status.

### `AiCliTaskExecutor`

Single shared execution entry for editor tools.

Flow:

1. create working directory
2. call `PrepareInputs`
3. call `BuildPrompt`
4. write prompt file
5. resolve and start CLI
6. pipe prompt into stdin
7. enter running state
8. poll `BuildRunningProgress`
9. poll `TryFinalize`
10. complete or fail

Output and error callbacks registered through `BeginOutputReadLine` and `BeginErrorReadLine`
run on .NET thread-pool threads. These callbacks must only perform pure .NET work
such as file I/O, JSON parsing, and state updates protected by the executor lock.
They must not call Unity main-thread APIs such as `EditorApplication.timeSinceStartup`.

Calling Unity APIs from those callbacks throws off the main thread and terminates
the async output reader. Once the reader is dead, the child CLI process can fill
the stdout/stderr pipe buffer and then block forever while the editor UI remains
stuck in the running phase. Progress timestamps and UI-facing state should be
updated from the editor update loop instead.

## Result Delivery Contract

The shared layer should treat output files as the canonical result channel.

Provider text replies are only diagnostics and progress hints.

Business modules should prefer:

- `output/*.json`
- `output/result.json`
- `output/patch.json`
- other deterministic files inside the task working directory

## Working Directory Isolation Contract

Every AI CLI task must treat `AiCliTaskContext.WorkingDirectory` as the only
workspace that the CLI is allowed to read from and write to.

The task prompt must not instruct the CLI to write directly into the Unity
project root, `Assets/`, `ProjectSettings/`, `Packages/`, or any other project
directory outside the task working directory.

Business task definitions must pass only paths inside `WorkingDirectory` to the
CLI for input, temporary files, and output files. The shared executor already
creates `AiCliTaskContext.OutputDirectory` as `WorkingDirectory/output`; business
tasks should use that directory as their canonical CLI result location.

If a business tool needs to apply AI output to project assets, it must first
validate the result files inside `WorkingDirectory`. Only after validation
succeeds may Unity editor code copy, import, patch, or otherwise apply those
results to project paths. The apply step is business-owned and must be performed
by local editor code, not by the CLI.

## Platform Policy

- support Windows / Linux / macOS
- prefer direct process + stdin over shell-generated behavior in framework code
- allow the AI CLI to create and execute helper scripts inside its working directory as part of its own task execution
- framework code does not generate business shell scripts for the AI

## Localization Migration Target

Localization should be split into:

- service wrapper: editor-facing entry
- task definition: AI task contract
- input builder: source file preparation
- output validator: JSON validation
- apply service: Excel sync

The current localization-specific process and provider logic should move into the shared AI CLI layer wherever it is business-agnostic.
