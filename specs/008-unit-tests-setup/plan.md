# Implementation Plan: Set Up Unit Tests

**Branch**: `008-unit-tests-setup` | **Date**: 2026-04-12 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/008-unit-tests-setup/spec.md`

## Summary

Set up a greenfield unit test suite for the MoriiCoffee .NET 10 Clean Architecture backend.  
Two xUnit test projects will be created — `MoriiCoffee.Domain.Tests` and `MoriiCoffee.Application.Tests` — using Moq 4.20.72 for mocking and FluentAssertions 8.9.0 for assertions. Tests will cover all four domain aggregates, all FluentValidation validators, all command/query handlers, and all AutoMapper profiles. No external dependencies (database, email, storage) are required to run any test.

## Technical Context

**Language/Version**: C# / .NET 10.0 (`net10.0`)  
**Primary Dependencies**: xUnit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.4.0, Moq 4.20.72, FluentAssertions 8.9.0  
**Storage**: N/A — all persistence dependencies are mocked via `Mock<IUnitOfWork>()`  
**Testing**: `dotnet test source/` — no external services required  
**Target Platform**: Cross-platform (macOS/Linux/Windows, .NET 10 SDK)  
**Project Type**: Two xUnit test class libraries  
**Performance Goals**: All unit tests complete within 30 seconds  
**Constraints**: `TreatWarningsAsErrors=true` inherited from `Directory.Build.props`; no external service, network, or container needed to run tests  
**Scale/Scope**: 2 test projects covering 4 aggregates, 12+ validators, 25+ command handlers, 11+ query handlers, 4 AutoMapper profiles

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Plan Mode Default | ✓ PASS | Planning complete before any code written |
| II. Verification Before Done | ✓ PASS | `dotnet test source/` must pass before tasks marked complete |
| III. Simplicity First | ✓ PASS | Two flat test projects; no shared test utilities project; no abstraction until duplication justifies it |
| IV. Subagent Strategy | ✓ PASS | Research and codebase exploration delegated to subagents in Phase 0 |
| V. Self-Improvement Loop | ✓ PASS | `tasks/lessons.md` updated after corrections |
| VI. Autonomous Execution | ✓ PASS | All decisions resolved; plan executable without further clarification |

**Gate result**: All principles satisfied. No violations to document.

## Project Structure

### Documentation (this feature)

```text
specs/008-unit-tests-setup/
├── plan.md              # This file
├── research.md          # Phase 0 output — all technology decisions
├── data-model.md        # Phase 1 output — test project structure and class inventory
├── quickstart.md        # Phase 1 output — how to run tests
└── tasks.md             # Phase 2 output (/speckit.tasks command — NOT created here)
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Domain/                        # (existing, unchanged)
├── MoriiCoffee.Domain.Shared/                 # (existing, unchanged)
├── MoriiCoffee.Application/                   # (existing, unchanged)
├── MoriiCoffee.Infrastructure/                # (existing, unchanged)
├── MoriiCoffee.Infrastructure.Persistence/    # (existing, unchanged)
├── MoriiCoffee.Presentation/                  # (existing, unchanged)
│
├── MoriiCoffee.Domain.Tests/                  # NEW
│   ├── MoriiCoffee.Domain.Tests.csproj
│   └── Aggregates/
│       ├── UserAggregateTests.cs
│       ├── ProductAggregateTests.cs
│       ├── CategoryAggregateTests.cs
│       └── BannerAggregateTests.cs
│
└── MoriiCoffee.Application.Tests/             # NEW
    ├── MoriiCoffee.Application.Tests.csproj
    ├── Commands/
    │   ├── Auth/
    │   │   ├── SignUpCommandHandlerTests.cs
    │   │   ├── SignUpCommandValidatorTests.cs
    │   │   ├── SignInCommandHandlerTests.cs
    │   │   ├── SignInCommandValidatorTests.cs
    │   │   ├── RefreshTokenCommandHandlerTests.cs
    │   │   ├── ForgotPasswordCommandHandlerTests.cs
    │   │   ├── ForgotPasswordCommandValidatorTests.cs
    │   │   ├── ResetPasswordCommandHandlerTests.cs
    │   │   ├── ResetPasswordCommandValidatorTests.cs
    │   │   └── ExternalLoginCommandHandlerTests.cs
    │   ├── Product/
    │   │   ├── CreateProductCommandHandlerTests.cs
    │   │   ├── CreateProductCommandValidatorTests.cs
    │   │   ├── UpdateProductCommandHandlerTests.cs
    │   │   ├── UpdateProductCommandValidatorTests.cs
    │   │   ├── DeleteProductCommandHandlerTests.cs
    │   │   ├── UploadProductImagesCommandHandlerTests.cs
    │   │   └── ReorderProductImagesCommandHandlerTests.cs
    │   ├── ProductVariant/
    │   │   ├── CreateProductVariantCommandHandlerTests.cs
    │   │   ├── CreateProductVariantCommandValidatorTests.cs
    │   │   ├── UpdateProductVariantCommandHandlerTests.cs
    │   │   └── DeleteProductVariantCommandHandlerTests.cs
    │   ├── Category/
    │   │   ├── CreateCategoryCommandHandlerTests.cs
    │   │   ├── CreateCategoryCommandValidatorTests.cs
    │   │   ├── UpdateCategoryCommandHandlerTests.cs
    │   │   ├── UpdateCategoryCommandValidatorTests.cs
    │   │   └── DeleteCategoryCommandHandlerTests.cs
    │   ├── User/
    │   │   ├── UpdateProfileCommandHandlerTests.cs
    │   │   ├── UpdateProfileCommandValidatorTests.cs
    │   │   ├── ChangeAvatarCommandHandlerTests.cs
    │   │   ├── ChangePasswordCommandHandlerTests.cs
    │   │   ├── ChangePasswordCommandValidatorTests.cs
    │   │   └── AssignRolesCommandHandlerTests.cs
    │   └── Banner/
    │       ├── CreateBannerCommandHandlerTests.cs
    │       ├── UpdateBannerCommandHandlerTests.cs
    │       └── DeleteBannerCommandHandlerTests.cs
    ├── Queries/
    │   ├── Product/
    │   │   ├── GetPaginatedProductsQueryHandlerTests.cs
    │   │   └── GetProductByIdQueryHandlerTests.cs
    │   ├── ProductVariant/
    │   │   ├── GetVariantsByProductIdQueryHandlerTests.cs
    │   │   └── GetVariantByIdQueryHandlerTests.cs
    │   ├── Category/
    │   │   ├── GetAllCategoriesQueryHandlerTests.cs
    │   │   └── GetCategoryByIdQueryHandlerTests.cs
    │   ├── User/
    │   │   ├── GetMyProfileQueryHandlerTests.cs
    │   │   ├── GetPaginatedUsersQueryHandlerTests.cs
    │   │   └── GetUserByIdQueryHandlerTests.cs
    │   └── Banner/
    │       ├── GetAllBannersQueryHandlerTests.cs
    │       └── GetBannerByIdQueryHandlerTests.cs
    └── Mappings/
        ├── ProductMapperTests.cs
        ├── CategoryMapperTests.cs
        ├── UserMapperTests.cs
        └── BannerMapperTests.cs
```

**Structure Decision**: Two flat test projects mirroring production project naming. No shared test utilities project — shared mock factory methods are inline `static` helpers within each test class. This follows the constitution's "Simplicity First" principle: no abstraction until there is clear duplication across 3+ classes.

## Complexity Tracking

> No constitution violations — no entries required.
