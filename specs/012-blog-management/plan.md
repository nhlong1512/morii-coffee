# Implementation Plan: Blog Management

**Branch**: `012-blog-management` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/012-blog-management/spec.md`

## Summary

Implement a minimal internal blog CMS for Morii Coffee that supports:

- blog post lifecycle management (`Draft`, `Published`, `Archived`)
- public storefront blog browsing by slug, featured state, and category
- internal category management with safe deletion rules
- simple operational curation via `displayOrder`
- role split where `ADMIN` manages content and `STAFF` assists with viewing and reordering

The technical approach is:

- add a dedicated **Blog bounded context** instead of overloading the existing product/category aggregates
- model `BlogPost` and `BlogCategory` as separate aggregates with a join entity for many-to-many assignment
- reuse the existing **ApiResponse**, **Pagination**, CQRS, repository, and controller patterns already present in Morii
- reuse the existing generic file-upload capability and add a dedicated public container/folder for blog assets
- store rich text as **canonical editor JSON plus rendered HTML snapshot** for reliable editing and storefront rendering
- expose separate **public** and **admin** API surfaces so storefront consumers never need admin filtering logic

This plan creates Phase 0 and Phase 1 design artefacts only. Implementation tasks are produced by `/speckit-tasks`.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) across the backend projects  
**Primary Dependencies**: Existing stack only for backend implementation: ASP.NET Core Web API, MediatR, FluentValidation, AutoMapper, EF Core 10, Npgsql, Swashbuckle, AWS S3/MinIO file services. Frontend consumer is expected to use Tiptap + React Hook Form + Zod, but those stay outside this repo's implementation scope for now.  
**Storage**: PostgreSQL via EF Core + Npgsql; public asset storage via the existing S3/MinIO-backed file service  
**Testing**: xUnit + Moq + FluentAssertions in `MoriiCoffee.Application.Tests`, with handler/query/validator coverage and endpoint verification through the existing backend testing workflow  
**Target Platform**: ASP.NET Core backend service in Docker, consumed by Morii admin and storefront clients  
**Project Type**: Backend web service with documented frontend integration contracts  
**Performance Goals**:
- public blog list/detail responses ≤ 500 ms p95 for the initial dataset
- admin list/filter responses ≤ 700 ms p95 for the initial dataset
- reorder/status updates ≤ 300 ms p95
**Constraints**:
- must preserve existing Clean Architecture layering
- must keep changes isolated to the new blog module plus minimal shared infrastructure updates
- must reuse the standardized `ApiResponse` and `Pagination` contracts
- must keep public visibility rules simple and deterministic
- must not introduce approval workflow, scheduled publishing, or author-role complexity
**Scale/Scope**:
- expected launch scope: up to 50 published posts and 20 categories
- content volume per post can include long-form rich text and a single cover image
- initial implementation likely touches 30–45 files across Domain, Application, Infrastructure.Persistence, Presentation, tests, and docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance check | Status |
|---|---|---|
| **I. Plan Mode Default** | This is a non-trivial, multi-layer feature with API, persistence, role rules, and public/storefront behavior. Planning is required and now in place. | ✅ |
| **II. Verification Before Done** | Implementation will require build/test evidence plus endpoint-level verification for public/admin blog flows before completion. Quickstart defines the verification path. | ✅ |
| **III. Simplicity First & Minimal Impact** | The design adds a dedicated blog module without refactoring unrelated product/order/payment code. Shared changes are limited to file container constants, DI registration, `ApplicationDbContext`, `UnitOfWork`, and controller routing surface. | ✅ |
| **IV. Subagent Strategy & Delegation** | Research and architecture context can be delegated in future phases, but this plan keeps the main context focused on design decisions. | ✅ |
| **V. Self-Improvement Loop** | Any user correction during implementation will be captured in `tasks/lessons.md` per constitution. | ✅ |
| **VI. Autonomous Execution with Concise Communication** | Planning artifacts are being produced end-to-end with minimal user friction. | ✅ |
| **Tech Stack Constraints** | The plan stays on the current backend stack: ASP.NET Core, EF Core, PostgreSQL, existing file service, existing auth roles. No framework changes introduced. | ✅ |
| **Minimal Impact to Existing Features** | The blog module is additive. Existing product categories, banners, files, auth, and public storefront APIs remain intact. | ✅ |

**Result (pre-design)**: No constitutional violations. No entries in *Complexity Tracking*.

### Post-design re-evaluation

After Phase 1 artefacts (`research.md`, `data-model.md`, `contracts/*`, `quickstart.md`) were authored:

| Principle | Re-check finding |
|---|---|
| Simplicity / Minimal impact | Confirmed. The design introduces dedicated blog entities instead of mutating unrelated catalog models, and reuses shared upload, auth, pagination, response, and soft-delete patterns. |
| Verification before done | Confirmed. `quickstart.md` defines concrete API verification for admin/public flows and the research/data model define the required unit/integration coverage areas. |
| Layering discipline | Confirmed. The design keeps blog rules in Domain/Application, persistence in Infrastructure.Persistence, and HTTP surface in Presentation, consistent with current modules. |

No new constitutional violations were introduced by the design.

## Project Structure

### Documentation (this feature)

```text
specs/012-blog-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── README.md
│   ├── public-blog-posts.md
│   ├── admin-blog-posts.md
│   └── admin-blog-categories.md
├── checklists/
│   └── requirements.md
└── tasks.md                # Phase 2 output — produced by /speckit-tasks
```

### Source Code (repository root)

```text
source/
├── MoriiCoffee.Domain/
│   ├── Aggregates/
│   │   ├── BlogPostAggregate/
│   │   │   ├── BlogPost.cs
│   │   │   └── Entities/
│   │   │       └── BlogPostCategory.cs
│   │   └── BlogCategoryAggregate/
│   │       └── BlogCategory.cs
│   ├── Repositories/
│   │   ├── IBlogPostsRepository.cs
│   │   └── IBlogCategoriesRepository.cs
│   └── SeedWork/
│
├── MoriiCoffee.Domain.Shared/
│   ├── Enums/
│   │   └── Blog/
│   │       └── EBlogPostStatus.cs
│   └── Constants/
│       └── FileContainers.cs                   # add BLOGS public container
│
├── MoriiCoffee.Application/
│   ├── Commands/
│   │   ├── BlogPost/
│   │   │   ├── CreateBlogPost/
│   │   │   ├── UpdateBlogPost/
│   │   │   ├── DeleteBlogPost/
│   │   │   ├── UpdateBlogPostStatus/
│   │   │   └── ReorderBlogPosts/
│   │   └── BlogCategory/
│   │       ├── CreateBlogCategory/
│   │       ├── UpdateBlogCategory/
│   │       ├── DeleteBlogCategory/
│   │       └── ReorderBlogCategories/
│   ├── Queries/
│   │   ├── BlogPost/
│   │   │   ├── GetPublicBlogPosts/
│   │   │   ├── GetPublicBlogPostBySlug/
│   │   │   ├── GetFeaturedBlogPosts/
│   │   │   ├── GetAdminBlogPosts/
│   │   │   └── GetAdminBlogPostById/
│   │   └── BlogCategory/
│   │       ├── GetPublicBlogCategories/
│   │       └── GetAdminBlogCategories/
│   └── SeedWork/
│       └── DTOs/
│           └── Blog/
│               ├── BlogCategoryDto.cs
│               ├── BlogPostSummaryDto.cs
│               ├── BlogPostDetailDto.cs
│               ├── CreateBlogPostDto.cs
│               ├── UpdateBlogPostDto.cs
│               ├── UpdateBlogPostStatusDto.cs
│               ├── ReorderBlogPostsDto.cs
│               ├── CreateBlogCategoryDto.cs
│               ├── UpdateBlogCategoryDto.cs
│               └── ReorderBlogCategoriesDto.cs
│
├── MoriiCoffee.Infrastructure.Persistence/
│   ├── Configurations/
│   │   ├── BlogPostConfiguration.cs
│   │   ├── BlogCategoryConfiguration.cs
│   │   └── BlogPostCategoryConfiguration.cs
│   ├── Repositories/
│   │   ├── BlogPostsRepository.cs
│   │   └── BlogCategoriesRepository.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs            # add DbSet<BlogPost>, DbSet<BlogCategory>, DbSet<BlogPostCategory>
│   ├── Migrations/
│   │   └── <timestamp>_AddBlogManagement.cs
│   └── SeedWork/
│
├── MoriiCoffee.Infrastructure/
│   └── Configurations/
│
├── MoriiCoffee.Presentation/
│   └── Controllers/
│       ├── BlogPostsController.cs             # public endpoints
│       ├── AdminBlogPostsController.cs
│       └── AdminBlogCategoriesController.cs
│
└── MoriiCoffee.Application.Tests/
    ├── Commands/
    │   ├── BlogPost/
    │   └── BlogCategory/
    └── Queries/
        ├── BlogPost/
        └── BlogCategory/
```

**Structure Decision**: Create a dedicated blog module with two aggregates (`BlogPost`, `BlogCategory`) plus a join entity, following the same layering style as existing catalog and payment modules. Keep public and admin HTTP surfaces separate because their visibility, authorization, and filtering rules differ materially.

## Complexity Tracking

No constitutional violations to justify. Section intentionally empty.
