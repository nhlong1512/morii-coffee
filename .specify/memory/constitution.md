<!--
Sync Impact Report:
Version change: [UNVERSIONED] → 1.0.0
Modified principles: N/A (Initial constitution)
Added sections:
  - Core Principles (6 principles)
  - Tech Stack Constraints
  - Development Workflow
  - Governance
Templates requiring updates:
  ✅ plan-template.md - Constitution Check section already present
  ✅ spec-template.md - Aligned with user story structure
  ✅ tasks-template.md - Aligned with verification and testing requirements
Follow-up TODOs: None
-->

# Morii Coffee Constitution

## Core Principles

### I. Plan Mode Default

**Rule**: Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions).

All development work requiring more than simple edits MUST begin with planning. Plans are written to `tasks/todo.md` with checkable items before implementation begins. If something fails or goes sideways during implementation, STOP immediately and re-plan rather than continuing blindly.

**Rationale**: Planning reduces ambiguity, prevents rework, and ensures architectural decisions are reviewed before code is written. This principle enforces the "measure twice, cut once" discipline that distinguishes senior engineers from junior ones.

### II. Verification Before Done (NON-NEGOTIABLE)

**Rule**: Never mark a task complete without proving it works through testing, logs, or demonstration.

Every task completion MUST include verification evidence. For code changes: diff behavior, run tests, check logs, verify UI. For API changes: demonstrate endpoints work. For bug fixes: prove the bug no longer reproduces. Ask: "Would a staff engineer approve this PR?"

**Rationale**: Unverified work creates technical debt and erodes trust. This principle ensures quality gates are enforced at the lowest level—individual task completion—rather than discovered later in code review or production.

### III. Simplicity First & Minimal Impact

**Rule**: Make every change as simple as possible, impacting minimal code.

Changes MUST only touch what is necessary for the task. No refactoring unrelated code unless explicitly requested. No adding features beyond requirements. No over-engineering for hypothetical future needs. Three similar lines of code is better than a premature abstraction.

**Rationale**: Unnecessary changes increase risk, review burden, and maintenance cost. YAGNI (You Aren't Gonna Need It) and minimal diffs are hallmarks of professional engineering discipline.

### IV. Subagent Strategy & Delegation

**Rule**: Use subagents liberally to keep main context focused; offload research, exploration, and parallel analysis.

Delegate one focused task per subagent with clear input context and structured output expectations. Use subagents for codebase exploration, research, and parallel execution to preserve the main conversation context for high-level decision-making.

**Rationale**: Context window management is critical for complex projects. Subagents enable better scaling and allow the main agent to maintain architectural focus rather than getting lost in implementation details.

### V. Self-Improvement Loop

**Rule**: After ANY user correction, update `tasks/lessons.md` with the failure pattern and prevention rule.

When corrected, document the mistake pattern immediately. Write rules that prevent exact repetition. Ruthlessly iterate until mistake rate drops to zero. Review `lessons.md` at the start of every session.

**Rationale**: Learning from mistakes is mandatory for improvement. Systematic capture of failure patterns creates institutional memory and prevents recurring errors across sessions.

### VI. Autonomous Execution with Concise Communication

**Rule**: Execute fixes autonomously; communicate results concisely without fluff or apologies.

When given a bug report or failing test: just fix it. Point at logs/errors, identify root cause, implement elegant solution. Communication MUST be direct: state what was done, why, and the result. No unnecessary conversational filler, no excessive apologies, no over-explanation.

**Rationale**: Users value action over conversation. Concise, factual communication respects user time and demonstrates confidence. Professional execution speaks louder than verbose explanations.

## Tech Stack Constraints

The following technology choices are standardized for this project and MUST NOT be changed without explicit architectural approval:

**Frontend (Next.js)**:
- Framework: Next.js 16 (App Router, `src/` directory structure)
- Language: TypeScript strict mode
- Styling: Tailwind CSS v4 with CSS variables (shadcn design system)
- State: Zustand with persist middleware
- i18n: next-intl (VI/EN locales)
- UI Primitives: Radix UI + custom shadcn components
- Package Manager: pnpm

**Backend (.NET 8 Clean Architecture)**:
- Base URL: `http://localhost:8002/api` (development)
- Auth: JWT Bearer tokens (access + refresh)
- Architecture: Clean Architecture pattern with domain-driven design

## Development Workflow

### Task Management (Definition of Done)

Every feature implementation MUST follow this workflow:

1. **Plan First**: Write plan to `tasks/todo.md` with checkable items
2. **Verify Plan**: Check with user before implementation (if architectural)
3. **Track Progress**: Mark items complete as work progresses
4. **Git Discipline**: Make atomic, descriptive commits after verifying each logical chunk
5. **Document Results**: Add review/summary section to `tasks/todo.md`
6. **Capture Lessons**: Update `tasks/lessons.md` after any corrections

### Git Commit Standards

- Atomic commits: one logical change per commit
- Descriptive messages: explain the "why" not just the "what"
- Verification before commit: ensure change works as intended
- Reference issues/tasks where applicable

## Governance

This constitution supersedes all other practices and serves as the single source of truth for development standards on the Morii Coffee project.

**Amendment Procedure**:
- Constitution changes require explicit documentation of rationale
- Version MUST be incremented per semantic versioning rules
- Dependent templates (`plan-template.md`, `spec-template.md`, `tasks-template.md`) MUST be updated for consistency
- All team members MUST be notified of constitutional amendments

**Compliance Review**:
- All PRs MUST verify compliance with constitutional principles
- Code reviews MUST explicitly check for violations
- Unjustified complexity MUST be rejected
- Constitutional violations MUST be documented or remediated

**Versioning Policy**:
- MAJOR version: Backward incompatible governance changes, principle removals/redefinitions
- MINOR version: New principles added, materially expanded guidance
- PATCH version: Clarifications, wording fixes, non-semantic refinements

**Runtime Guidance**: Use `CLAUDE.md` for day-to-day development workflow guidance. The constitution defines governance; CLAUDE.md provides operational procedures.

**Version**: 1.0.0 | **Ratified**: 2026-03-23 | **Last Amended**: 2026-03-23
