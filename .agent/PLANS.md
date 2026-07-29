# Codex Execution Plans

An ExecPlan is a self-contained, living implementation plan for a complex feature or multi-milestone change.

## When Required

Create an ExecPlan for:

- the first vertical-slice architecture;
- multi-system gameplay features;
- significant refactors;
- changes expected to span multiple sessions;
- work with important technical uncertainty.

Store plans under `.agent/plans/`.

## Required Properties

An ExecPlan must:

- explain the player-visible outcome first;
- assume the reader has only the repository and the plan;
- record repository findings rather than guessing;
- define important terms in plain language;
- separate facts, decisions, and assumptions;
- contain observable acceptance criteria;
- include automated and manual validation;
- identify risks and fallback approaches;
- be updated as work progresses;
- record important decisions and discoveries;
- leave the repository in a demonstrably working state at each completed milestone where practical.

## Required Sections

Use these sections:

# Title

## Purpose and Player Outcome

What becomes possible and how someone can see it working.

## Current Repository Findings

Unity version, packages, scenes, settings, existing code, tests, and constraints discovered from the actual repository.

## Scope

What is included and explicitly excluded.

## Architecture Proposal

Major components, responsibilities, data flow, and logic/presentation boundaries.

## Alternatives Considered

At least the meaningful alternatives and why they were accepted or rejected.

## Milestones

Each milestone must include:

- goal;
- files/systems expected to change;
- implementation steps;
- acceptance criteria;
- automated validation;
- manual Unity verification;
- expected playable result.

## Risks and Unknowns

Technical uncertainty, Unity-specific risks, performance risks, and product assumptions.

## Progress

A checkbox list updated after each meaningful step.

## Decision Log

Dated decisions with reasoning.

## Discoveries

Unexpected repository facts, bugs, limitations, or useful observations.

## Validation Record

Commands/tests run, results, manual checks completed, and checks still pending.

## Final Outcome

What was delivered, known limitations, and recommended next work.

## Planning Rules

While creating the initial plan:

- inspect first;
- do not modify production code, scenes, prefabs, packages, or ProjectSettings;
- do not invent test commands that do not exist;
- flag conflicts between the documentation and repository;
- make the earliest milestones playable;
- avoid speculative frameworks and unnecessary dependencies.

While implementing:

- keep this document current;
- do not silently change scope;
- record major architectural decisions in `Docs/DECISIONS.md`;
- validate each milestone before claiming completion;
- clearly identify manual checks that Codex cannot perform.
