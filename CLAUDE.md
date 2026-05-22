# Morii Coffee - Workflow Orchestration
## Workflow Orchestration

### 0. Read Skill Files Before Anything
- Before implementing ANYTHING, always read the relevant skill file first.
- For backend tasks: read `/clean-architecture-skill` to understand the architecture, 
  conventions, project structure, and patterns before writing a single line of code.
- Never skip this step — even for small changes. The skill file is the source of truth 
  for how this project is structured and how code should be written.
- If no skill file exists for the task, ask the user before proceeding.

### 1. Plan Mode Default
- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions).
- If something goes sideways, STOP and re-plan immediately - don't keep pushing blindly.
- Use plan mode for verification steps, not just building.
- Write detailed specs upfront to reduce ambiguity before writing any code.

### 2. Subagent Strategy & Delegation
- Use subagents liberally to keep the main context window clean and focused.
- Offload research, exploration, and parallel analysis to subagents.
- Pass clear input context and demand structured output from subagents.
- One task per subagent for focused execution.

### 3. Self-Improvement Loop
- After ANY correction from the user: update `tasks/lessons.md` with the failure pattern.
- Write rules for yourself that prevent the exact same mistake.
- Ruthlessly iterate on these lessons until the mistake rate drops to zero.
- Review `lessons.md` at the start of every session for the relevant project.

### 4. Verification Before Done
- Never mark a task complete without proving it works.
- Diff behavior between main and your changes when relevant.
- Ask yourself: "Would a staff engineer approve this PR?"
- Run tests, check logs, verify UI (if applicable), and demonstrate correctness.
- Ensure code passes local linting and formatting standards.

### 5. Demand Elegance (Balanced)
- For non-trivial changes: pause and ask "Is there a more elegant/performant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution."
- Skip this for simple, obvious fixes - don't over-engineer.
- Challenge your own work before presenting it to the user.

### 6. Autonomous Execution & Communication
- When given a bug report: just fix it. Don't ask for hand-holding.
- Point at logs, errors, failing tests - then resolve them.
- Go fix failing CI tests without being told how.
- **Communication:** Be concise. No fluff, no apologies, no unnecessary conversational filler. Just state what was done, why, and the result.

### 7. Write Summary Docs Before Marking Complete
- Before marking ANY task as done, write two summary files documenting all changes made.
- One file in Vietnamese, one file in English.
- Save them under: `docs/explainations/`
- Naming convention: `summary-{feature-name}-{VN|ENG}.md`
  Example:
    docs/explainations/summary-product-images-update-VN.md
    docs/explainations/summary-product-images-update-ENG.md
- Each summary must cover:
  - What was implemented or changed and why
  - Files created or modified
  - Database changes (if any)
  - API changes (new or modified endpoints)
  - Business rules applied
  - How to verify / test the changes
- This is non-negotiable — no task is considered complete without both summary files.

## Task Management (Definition of Done)

1. **Plan First**: Write the plan to `tasks/todo.md` with checkable items.
2. **Verify Plan**: Check in with the user before starting implementation (if architectural).
3. **Track Progress**: Mark items complete as you go.
4. **Git Discipline**: Make atomic, descriptive Git commits after verifying each logical chunk.
5. **Document Results**: Add a quick review/summary section to `tasks/todo.md`.
6. **Capture Lessons**: Update `tasks/lessons.md` after any user corrections.

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary band-aids. Uphold Senior Developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid refactoring unrelated code unless explicitly asked.

## Run Project
```sh
cd deploy && bash run-docker-development.sh
```

## Active Technologies
- C# / .NET 8.0 + SendGrid SDK 9.29.3, ASP.NET Core Identity, MediatR, Serilog (002-remove-aws-ses)
- SQL Server (via Entity Framework Core), email configuration in appsettings.json (002-remove-aws-ses)
- C# / .NET 8.0 + brevo_csharp (official Brevo SDK), ASP.NET Core Identity, MediatR, Serilog (003-email-service-spec)
- SQL Server (via Entity Framework Core), embedded HTML email templates, email configuration in appsettings.json (003-email-service-spec)
- C# / .NET 8.0 + ASP.NET Core Identity, MediatR, FluentValidation, EF Core (004-email-only-auth)
- C# / .NET 8.0 + ASP.NET Core Identity, Microsoft.AspNetCore.Authentication.Google, MediatR, FluentValidation, EF Core (005-google-oauth)
- SQL Server (via Entity Framework Core) - AspNetUsers, AspNetUserLogins, AspNetUserTokens tables (005-google-oauth)
- C# / .NET 10.0 + ASP.NET Core 10.0.5, Entity Framework Core 10.0.5, MediatR 14.1.0, FluentValidation 12.1.1, Serilog 4.3.1, AutoMapper 16.1.1, Brevo SDK 1.1.2, MinIO 7.0.0, AWS SDK S3 4.0.20.2, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.5, Microsoft.AspNetCore.Authentication.Google 10.0.5, Swashbuckle.AspNetCore 6.7.2 (006-dotnet-10-upgrade)
- SQL Server (via Entity Framework Core 10.0.5), MinIO 7.0.0 (object storage), AWS S3 (fallback storage) (006-dotnet-10-upgrade)
- C# / .NET 10.0 (`net10.0`) + xUnit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.4.0, Moq 4.20.72, FluentAssertions 8.9.0 (008-unit-tests-setup)
- N/A — all persistence dependencies are mocked via `Mock<IUnitOfWork>()` (008-unit-tests-setup)
- C# / .NET 10.0 (`net10.0`) — confirmed in `MoriiCoffee.Presentation.csproj`. The constitution mentions .NET 8 but the repo has already migrated (feature `006-dotnet-10-upgrade`). (011-stripe-payment)
- PostgreSQL via EF Core 10.0.5 + Npgsql. Two new tables (`Payments`, `Refunds`, `PaymentWebhookEvents`). One column added to `Orders` (`PaymentStatus`). (011-stripe-payment)
- Stripe.net 47.x, Stripe webhook signature verification, Checkout Sessions (011-stripe-payment)
- TypeScript 5.x (frontend) / C# .NET 10.0 (backend) (014-my-wishlist)

## Recent Changes
- 006-dotnet-10-upgrade: Upgraded platform from .NET 8.0 to .NET 10.0, all Microsoft packages to 10.0.5, MediatR to 14.1.0, AutoMapper to 16.1.1, FluentValidation to 12.1.1, Minio to 7.0.0
- 002-remove-aws-ses: Added C# / .NET 8.0 + SendGrid SDK 9.29.3, ASP.NET Core Identity, MediatR, Serilog
