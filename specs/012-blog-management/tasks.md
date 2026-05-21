# Tasks: Blog Management

**Input**: Design documents from `/specs/012-blog-management/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Include focused backend tests because the plan and quickstart explicitly require build/test verification for command, query, validator, and role-sensitive behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependency on another incomplete task)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`, etc.)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the feature scaffolding and shared constants used across the module

- [X] T001 Create blog feature folder structure under `source/MoriiCoffee.Domain/Aggregates/BlogPostAggregate/`, `source/MoriiCoffee.Domain/Aggregates/BlogCategoryAggregate/`, `source/MoriiCoffee.Application/Commands/BlogPost/`, `source/MoriiCoffee.Application/Commands/BlogCategory/`, `source/MoriiCoffee.Application/Queries/BlogPost/`, `source/MoriiCoffee.Application/Queries/BlogCategory/`, and `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/`
- [X] T002 Add the `BLOGS` public container constant to `source/MoriiCoffee.Domain.Shared/Constants/FileContainers.cs`
- [X] T003 Add the blog post status enum in `source/MoriiCoffee.Domain.Shared/Enums/Blog/EBlogPostStatus.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core blog-domain, persistence, and mapping infrastructure that MUST exist before any user story can be implemented

**⚠️ CRITICAL**: No user story work should begin until this phase is complete

- [X] T004 [P] Create the `BlogPost` aggregate root in `source/MoriiCoffee.Domain/Aggregates/BlogPostAggregate/BlogPost.cs`
- [X] T005 [P] Create the `BlogCategory` aggregate root in `source/MoriiCoffee.Domain/Aggregates/BlogCategoryAggregate/BlogCategory.cs`
- [X] T006 [P] Create the `BlogPostCategory` join entity in `source/MoriiCoffee.Domain/Aggregates/BlogPostAggregate/Entities/BlogPostCategory.cs`
- [X] T007 [P] Add repository interfaces in `source/MoriiCoffee.Domain/Repositories/IBlogPostsRepository.cs` and `source/MoriiCoffee.Domain/Repositories/IBlogCategoriesRepository.cs`
- [X] T008 [P] Add EF Core entity configurations in `source/MoriiCoffee.Infrastructure.Persistence/Configurations/BlogPostConfiguration.cs`, `source/MoriiCoffee.Infrastructure.Persistence/Configurations/BlogCategoryConfiguration.cs`, and `source/MoriiCoffee.Infrastructure.Persistence/Configurations/BlogPostCategoryConfiguration.cs`
- [X] T009 Update `source/MoriiCoffee.Infrastructure.Persistence/Data/ApplicationDbContext.cs` with `DbSet<BlogPost>`, `DbSet<BlogCategory>`, and `DbSet<BlogPostCategory>`
- [X] T010 Create repository implementations in `source/MoriiCoffee.Infrastructure.Persistence/Repositories/BlogPostsRepository.cs` and `source/MoriiCoffee.Infrastructure.Persistence/Repositories/BlogCategoriesRepository.cs`
- [X] T011 Update `source/MoriiCoffee.Domain/SeedWork/Persistence/IUnitOfWork.cs` and `source/MoriiCoffee.Infrastructure.Persistence/SeedWork/UnitOfWork/UnitOfWork.cs` to expose blog repositories
- [X] T012 [P] Add shared blog DTOs in `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/BlogCategoryDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/BlogPostSummaryDto.cs`, and `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/BlogPostDetailDto.cs`
- [X] T013 [P] Add shared admin request DTOs in `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/CreateBlogPostDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/UpdateBlogPostDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/UpdateBlogPostStatusDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/ReorderBlogPostsDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/CreateBlogCategoryDto.cs`, `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/UpdateBlogCategoryDto.cs`, and `source/MoriiCoffee.Application/SeedWork/DTOs/Blog/ReorderBlogCategoriesDto.cs`
- [X] T014 Create the AutoMapper profile in `source/MoriiCoffee.Application/SeedWork/Mappings/BlogMapper.cs` and register it in `source/MoriiCoffee.Infrastructure/Configurations/MapperConfiguration.cs`
- [X] T015 Add the database migration for the blog module in `source/MoriiCoffee.Infrastructure.Persistence/Migrations/`

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 - Admin manages blog post lifecycle (Priority: P1) 🎯 MVP

**Goal**: Allow administrators to create, edit, publish, unpublish, archive, and soft-delete blog posts from the admin area

**Independent Test**: As `ADMIN`, create a draft post, update it, publish it, move it back to draft or archived, and soft-delete it while confirming visibility changes behave correctly

### Implementation for User Story 1

- [X] T016 [P] [US1] Implement create-post command files in `source/MoriiCoffee.Application/Commands/BlogPost/CreateBlogPost/CreateBlogPostCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogPost/CreateBlogPost/CreateBlogPostCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogPost/CreateBlogPost/CreateBlogPostCommandValidator.cs`
- [X] T017 [P] [US1] Implement update-post command files in `source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPost/UpdateBlogPostCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPost/UpdateBlogPostCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPost/UpdateBlogPostCommandValidator.cs`
- [X] T018 [P] [US1] Implement soft-delete-post command files in `source/MoriiCoffee.Application/Commands/BlogPost/DeleteBlogPost/DeleteBlogPostCommand.cs` and `source/MoriiCoffee.Application/Commands/BlogPost/DeleteBlogPost/DeleteBlogPostCommandHandler.cs`
- [X] T019 [P] [US1] Implement post-status command files in `source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPostStatus/UpdateBlogPostStatusCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPostStatus/UpdateBlogPostStatusCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPostStatus/UpdateBlogPostStatusCommandValidator.cs`
- [X] T020 [P] [US1] Implement reorder-posts command files in `source/MoriiCoffee.Application/Commands/BlogPost/ReorderBlogPosts/ReorderBlogPostsCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogPost/ReorderBlogPosts/ReorderBlogPostsCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogPost/ReorderBlogPosts/ReorderBlogPostsCommandValidator.cs`
- [X] T021 [P] [US1] Implement admin post queries in `source/MoriiCoffee.Application/Queries/BlogPost/GetAdminBlogPosts/GetAdminBlogPostsQuery.cs`, `source/MoriiCoffee.Application/Queries/BlogPost/GetAdminBlogPosts/GetAdminBlogPostsQueryHandler.cs`, `source/MoriiCoffee.Application/Queries/BlogPost/GetAdminBlogPostById/GetAdminBlogPostByIdQuery.cs`, and `source/MoriiCoffee.Application/Queries/BlogPost/GetAdminBlogPostById/GetAdminBlogPostByIdQueryHandler.cs`
- [X] T022 [US1] Add the admin post controller in `source/MoriiCoffee.Presentation/Controllers/AdminBlogPostsController.cs` with list, detail, create, update, delete, status, and reorder endpoints plus `ADMIN`/`STAFF` authorization rules
- [X] T023 [P] [US1] Add command and validator tests for admin post lifecycle in `source/MoriiCoffee.Application.Tests/Commands/BlogPost/CreateBlogPostCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/CreateBlogPostCommandValidatorTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/UpdateBlogPostCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/UpdateBlogPostCommandValidatorTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/DeleteBlogPostCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/UpdateBlogPostStatusCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/UpdateBlogPostStatusCommandValidatorTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogPost/ReorderBlogPostsCommandHandlerTests.cs`, and `source/MoriiCoffee.Application.Tests/Commands/BlogPost/ReorderBlogPostsCommandValidatorTests.cs`
- [X] T024 [P] [US1] Add admin post query tests in `source/MoriiCoffee.Application.Tests/Queries/BlogPost/GetAdminBlogPostsQueryHandlerTests.cs` and `source/MoriiCoffee.Application.Tests/Queries/BlogPost/GetAdminBlogPostByIdQueryHandlerTests.cs`

**Checkpoint**: User Story 1 is functional when an admin can fully manage blog post lifecycle from internal endpoints

---

## Phase 4: User Story 2 - Customer browses published blog content (Priority: P1)

**Goal**: Expose published blog content to the storefront through public list, detail, featured, and category views

**Independent Test**: Publish posts across categories, then confirm the public endpoints return only published content, resolve slug detail, and filter correctly by category and featured state

### Implementation for User Story 2

- [X] T025 [P] [US2] Implement the public blog list query in `source/MoriiCoffee.Application/Queries/BlogPost/GetPublicBlogPosts/GetPublicBlogPostsQuery.cs` and `source/MoriiCoffee.Application/Queries/BlogPost/GetPublicBlogPosts/GetPublicBlogPostsQueryHandler.cs`
- [X] T026 [P] [US2] Implement the public blog detail query in `source/MoriiCoffee.Application/Queries/BlogPost/GetPublicBlogPostBySlug/GetPublicBlogPostBySlugQuery.cs` and `source/MoriiCoffee.Application/Queries/BlogPost/GetPublicBlogPostBySlug/GetPublicBlogPostBySlugQueryHandler.cs`
- [X] T027 [P] [US2] Implement the featured-posts query in `source/MoriiCoffee.Application/Queries/BlogPost/GetFeaturedBlogPosts/GetFeaturedBlogPostsQuery.cs` and `source/MoriiCoffee.Application/Queries/BlogPost/GetFeaturedBlogPosts/GetFeaturedBlogPostsQueryHandler.cs`
- [X] T028 [P] [US2] Implement the public blog categories query in `source/MoriiCoffee.Application/Queries/BlogCategory/GetPublicBlogCategories/GetPublicBlogCategoriesQuery.cs` and `source/MoriiCoffee.Application/Queries/BlogCategory/GetPublicBlogCategories/GetPublicBlogCategoriesQueryHandler.cs`
- [X] T029 [US2] Add the public blog controller in `source/MoriiCoffee.Presentation/Controllers/BlogPostsController.cs` with published list, detail-by-slug, featured, and public category endpoints
- [X] T030 [P] [US2] Add public query tests in `source/MoriiCoffee.Application.Tests/Queries/BlogPost/GetPublicBlogPostsQueryHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Queries/BlogPost/GetPublicBlogPostBySlugQueryHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Queries/BlogPost/GetFeaturedBlogPostsQueryHandlerTests.cs`, and `source/MoriiCoffee.Application.Tests/Queries/BlogCategory/GetPublicBlogCategoriesQueryHandlerTests.cs`

**Checkpoint**: User Story 2 is functional when storefront consumers can browse only published content by list, slug, category, and featured state

---

## Phase 5: User Story 3 - Admin manages blog categories safely (Priority: P2)

**Goal**: Allow administrators to create, update, reorder, and safely delete blog categories without breaking linked content

**Independent Test**: As `ADMIN`, create categories, assign them to posts, verify delete is blocked while in use, then unlink and delete successfully

### Implementation for User Story 3

- [X] T031 [P] [US3] Implement create-category command files in `source/MoriiCoffee.Application/Commands/BlogCategory/CreateBlogCategory/CreateBlogCategoryCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogCategory/CreateBlogCategory/CreateBlogCategoryCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogCategory/CreateBlogCategory/CreateBlogCategoryCommandValidator.cs`
- [X] T032 [P] [US3] Implement update-category command files in `source/MoriiCoffee.Application/Commands/BlogCategory/UpdateBlogCategory/UpdateBlogCategoryCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogCategory/UpdateBlogCategory/UpdateBlogCategoryCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogCategory/UpdateBlogCategory/UpdateBlogCategoryCommandValidator.cs`
- [X] T033 [P] [US3] Implement delete-category command files in `source/MoriiCoffee.Application/Commands/BlogCategory/DeleteBlogCategory/DeleteBlogCategoryCommand.cs` and `source/MoriiCoffee.Application/Commands/BlogCategory/DeleteBlogCategory/DeleteBlogCategoryCommandHandler.cs`
- [X] T034 [P] [US3] Implement reorder-categories command files in `source/MoriiCoffee.Application/Commands/BlogCategory/ReorderBlogCategories/ReorderBlogCategoriesCommand.cs`, `source/MoriiCoffee.Application/Commands/BlogCategory/ReorderBlogCategories/ReorderBlogCategoriesCommandHandler.cs`, and `source/MoriiCoffee.Application/Commands/BlogCategory/ReorderBlogCategories/ReorderBlogCategoriesCommandValidator.cs`
- [X] T035 [P] [US3] Implement the admin categories query in `source/MoriiCoffee.Application/Queries/BlogCategory/GetAdminBlogCategories/GetAdminBlogCategoriesQuery.cs` and `source/MoriiCoffee.Application/Queries/BlogCategory/GetAdminBlogCategories/GetAdminBlogCategoriesQueryHandler.cs`
- [X] T036 [US3] Add the admin category controller in `source/MoriiCoffee.Presentation/Controllers/AdminBlogCategoriesController.cs` with list, create, update, delete, and reorder endpoints plus in-use delete validation behavior
- [X] T037 [P] [US3] Add category command/query tests in `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/CreateBlogCategoryCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/CreateBlogCategoryCommandValidatorTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/UpdateBlogCategoryCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/UpdateBlogCategoryCommandValidatorTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/DeleteBlogCategoryCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/ReorderBlogCategoriesCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/ReorderBlogCategoriesCommandValidatorTests.cs`, and `source/MoriiCoffee.Application.Tests/Queries/BlogCategory/GetAdminBlogCategoriesQueryHandlerTests.cs`

**Checkpoint**: User Story 3 is functional when admins can manage categories safely and deletion is blocked while categories are still in use

---

## Phase 6: User Story 4 - Staff supports operational curation without editing content (Priority: P3)

**Goal**: Allow `STAFF` to inspect blog/category records and reorder them without gaining edit, publish, or delete permissions

**Independent Test**: Sign in as `STAFF`, confirm list/detail/reorder actions succeed, and confirm create/update/delete/status actions are denied

### Implementation for User Story 4

- [X] T038 [US4] Tighten `ADMIN` vs `STAFF` authorization rules in `source/MoriiCoffee.Presentation/Controllers/AdminBlogPostsController.cs` and `source/MoriiCoffee.Presentation/Controllers/AdminBlogCategoriesController.cs` so staff can only read and reorder
- [X] T039 [P] [US4] Add role-sensitive authorization and reorder behavior tests in `source/MoriiCoffee.Application.Tests/Commands/BlogPost/ReorderBlogPostsCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Commands/BlogCategory/ReorderBlogCategoriesCommandHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Queries/BlogPost/GetAdminBlogPostsQueryHandlerTests.cs`, `source/MoriiCoffee.Application.Tests/Queries/BlogCategory/GetAdminBlogCategoriesQueryHandlerTests.cs`, and `source/MoriiCoffee.Application.Tests/Presentation/BlogAdminAuthorizationTests.cs`

**Checkpoint**: User Story 4 is functional when staff can curate ordering without gaining content management authority

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete mapping/tests/docs/verification that span multiple stories

- [X] T040 [P] Add blog mapper tests in `source/MoriiCoffee.Application.Tests/Mappings/BlogMapperTests.cs`
- [X] T041 [P] Add Swagger summaries, response annotations, and request binding polish in `source/MoriiCoffee.Presentation/Controllers/BlogPostsController.cs`, `source/MoriiCoffee.Presentation/Controllers/AdminBlogPostsController.cs`, and `source/MoriiCoffee.Presentation/Controllers/AdminBlogCategoriesController.cs`
- [X] T042 [P] Update the engineering verification guide in `specs/012-blog-management/quickstart.md` to reflect any endpoint or payload adjustments from implementation
- [X] T043 Run the full verification workflow from `specs/012-blog-management/quickstart.md` and record any required implementation follow-ups in `specs/012-blog-management/tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup**: No dependencies
- **Phase 2: Foundational**: Depends on Setup completion and blocks all user stories
- **Phase 3: User Story 1**: Depends on Foundational completion
- **Phase 4: User Story 2**: Depends on Foundational completion and benefits from User Story 1 data creation paths
- **Phase 5: User Story 3**: Depends on Foundational completion
- **Phase 6: User Story 4**: Depends on User Stories 1 and 3 controller/query surfaces existing
- **Phase 7: Polish**: Depends on all desired user stories being implemented

### User Story Dependencies

- **US1**: Can start immediately after Foundational and is the suggested first implementation slice
- **US2**: Can begin after Foundational, but is easiest to verify once US1 can create/publish content
- **US3**: Can begin after Foundational and can be implemented in parallel with US1/US2
- **US4**: Depends on admin post/category surfaces from US1 and US3

### Within Each User Story

- Commands/queries before controllers
- Validators alongside their command implementations
- Repository-backed logic before endpoint exposure
- Story-specific tests before final verification of that story

### Parallel Opportunities

- T004, T005, T006 can run in parallel
- T007, T008, T012, T013 can run in parallel after the aggregates exist
- Within US1, T016–T021 can be split across multiple contributors before T022
- Within US2, T025–T028 can run in parallel before T029
- Within US3, T031–T035 can run in parallel before T036
- Polish tasks T040–T042 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Parallel command/query work for admin post lifecycle
Task: "Implement create-post command files in source/MoriiCoffee.Application/Commands/BlogPost/CreateBlogPost/"
Task: "Implement update-post command files in source/MoriiCoffee.Application/Commands/BlogPost/UpdateBlogPost/"
Task: "Implement admin post queries in source/MoriiCoffee.Application/Queries/BlogPost/"

# Parallel test work once handlers exist
Task: "Add command and validator tests for admin post lifecycle in source/MoriiCoffee.Application.Tests/Commands/BlogPost/"
Task: "Add admin post query tests in source/MoriiCoffee.Application.Tests/Queries/BlogPost/"
```

---

## Parallel Example: User Story 3

```bash
# Parallel category application-layer work
Task: "Implement create-category command files in source/MoriiCoffee.Application/Commands/BlogCategory/CreateBlogCategory/"
Task: "Implement update-category command files in source/MoriiCoffee.Application/Commands/BlogCategory/UpdateBlogCategory/"
Task: "Implement the admin categories query in source/MoriiCoffee.Application/Queries/BlogCategory/GetAdminBlogCategories/"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate admin lifecycle end-to-end

### Recommended Incremental Delivery

1. Ship internal authoring capability with **US1**
2. Add storefront read surface with **US2**
3. Add safe category management with **US3**
4. Tighten staff curation boundaries with **US4**
5. Finish mapper/docs/verification polish

### Suggested MVP Scope

The thinnest meaningful MVP is **User Story 1**.  
The first production-credible increment is **User Stories 1 + 2** together.

## Notes

- All tasks use explicit file paths or target directories
- `[P]` means the task can be worked on independently without waiting on another incomplete task in the same phase
- The `setup-tasks.sh` script emitted a non-blocking shell warning but still returned valid task-generation context; no follow-up is required unless the Speckit scripts themselves are being maintained
