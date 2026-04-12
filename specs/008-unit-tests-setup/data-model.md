# Data Model: Set Up Unit Tests

**Feature**: 008-unit-tests-setup  
**Date**: 2026-04-12

> This feature introduces no new domain entities or database changes.  
> The "data model" here describes the **test project structure** — how test assemblies, classes, and shared helpers are organized.

---

## Test Projects

### MoriiCoffee.Domain.Tests

| Property | Value |
|----------|-------|
| Project type | xUnit test library |
| Target framework | net10.0 |
| References | MoriiCoffee.Domain |
| External packages | xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.4.0, Moq 4.20.72, FluentAssertions 8.9.0 |

**Test classes and coverage**:

| Test Class | Aggregate Under Test | Domain Methods Covered |
|------------|---------------------|----------------------|
| `UserAggregateTests` | `User` | `UpdateProfile()`, `SetAvatar()`, `Activate()`, `Deactivate()`, `RaiseDomainEvent()`, `GetDomainEvents()`, `ClearDomainEvents()` |
| `ProductAggregateTests` | `Product` | Property initialization, `RaiseDomainEvent()`, `GetDomainEvents()`, `ClearDomainEvents()` |
| `CategoryAggregateTests` | `Category` | Property initialization, status transitions |
| `BannerAggregateTests` | `Banner` | Property initialization, active status |

---

### MoriiCoffee.Application.Tests

| Property | Value |
|----------|-------|
| Project type | xUnit test library |
| Target framework | net10.0 |
| References | MoriiCoffee.Application, MoriiCoffee.Domain, MoriiCoffee.Domain.Shared |
| External packages | xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.4.0, Moq 4.20.72, FluentAssertions 8.9.0 |

**Validator test classes**:

| Test Class | Validator Under Test | Rules Covered |
|------------|---------------------|--------------|
| `CreateProductCommandValidatorTests` | `CreateProductCommandValidator` | Name required/maxlen, Slug format regex, BasePrice ≥ 0, CategoryIds not empty, DisplayOrder ≥ 0 |
| `UpdateProductCommandValidatorTests` | `UpdateProductCommandValidator` | Same as Create for applicable fields |
| `CreateProductVariantCommandValidatorTests` | `CreateProductVariantCommandValidator` | All rules |
| `CreateCategoryCommandValidatorTests` | `CreateCategoryCommandValidator` | Name required/maxlen, DisplayOrder ≥ 0 |
| `UpdateCategoryCommandValidatorTests` | `UpdateCategoryCommandValidator` | Same as Create |
| `CreateBannerCommandValidatorTests` | `CreateBannerCommandValidator` | All rules |
| `SignUpCommandValidatorTests` | `SignUpCommandValidator` | Email format, PhoneNumber regex, Password complexity (uppercase, lowercase, digit, special), UserName format/length |
| `SignInCommandValidatorTests` | `SignInCommandValidator` | Email required, Password required |
| `ForgotPasswordCommandValidatorTests` | `ForgotPasswordCommandValidator` | Email required/format |
| `ResetPasswordCommandValidatorTests` | `ResetPasswordCommandValidator` | Token required, Password complexity |
| `UpdateProfileCommandValidatorTests` | `UpdateProfileCommandValidator` | FullName maxlen, Bio maxlen |
| `ChangePasswordCommandValidatorTests` | `ChangePasswordCommandValidator` | Password complexity |

**Command handler test classes**:

| Test Class | Handler Under Test | Mocked Dependencies | Scenarios |
|------------|-------------------|---------------------|-----------|
| `CreateProductCommandHandlerTests` | `CreateProductCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success (no thumbnail), success (with thumbnail), invalid category ID, slug already exists |
| `UpdateProductCommandHandlerTests` | `UpdateProductCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success, product not found |
| `DeleteProductCommandHandlerTests` | `DeleteProductCommandHandler` | `IUnitOfWork` | Success (soft delete), product not found |
| `CreateProductVariantCommandHandlerTests` | `CreateProductVariantCommandHandler` | `IUnitOfWork`, `IMapper` | Success, product not found |
| `UpdateProductVariantCommandHandlerTests` | `UpdateProductVariantCommandHandler` | `IUnitOfWork`, `IMapper` | Success, variant not found |
| `DeleteProductVariantCommandHandlerTests` | `DeleteProductVariantCommandHandler` | `IUnitOfWork` | Success, variant not found |
| `UploadProductImagesCommandHandlerTests` | `UploadProductImagesCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success (multiple files), product not found |
| `ReorderProductImagesCommandHandlerTests` | `ReorderProductImagesCommandHandler` | `IUnitOfWork` | Success, product not found |
| `CreateCategoryCommandHandlerTests` | `CreateCategoryCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success (no icon), success (with icon), CommitAsync called once |
| `UpdateCategoryCommandHandlerTests` | `UpdateCategoryCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success, category not found |
| `DeleteCategoryCommandHandlerTests` | `DeleteCategoryCommandHandler` | `IUnitOfWork` | Success, category not found |
| `CreateBannerCommandHandlerTests` | `CreateBannerCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success, CommitAsync called once |
| `UpdateBannerCommandHandlerTests` | `UpdateBannerCommandHandler` | `IUnitOfWork`, `IFileService`, `IMapper` | Success, banner not found |
| `DeleteBannerCommandHandlerTests` | `DeleteBannerCommandHandler` | `IUnitOfWork` | Success, banner not found |
| `SignUpCommandHandlerTests` | `SignUpCommandHandler` | `UserManager<User>`, `ITokenService`, `IEmailService`, `IMapper` | Success (new user), email already exists, phone already exists, UserManager.CreateAsync fails |
| `SignInCommandHandlerTests` | `SignInCommandHandler` | `UserManager<User>`, `ITokenService`, `IMapper` | Success, user not found, invalid password |
| `RefreshTokenCommandHandlerTests` | `RefreshTokenCommandHandler` | `UserManager<User>`, `ITokenService` | Success, invalid token, user not found |
| `ForgotPasswordCommandHandlerTests` | `ForgotPasswordCommandHandler` | `UserManager<User>`, `IEmailService` | Success (sends email), user not found (no error, silent) |
| `ResetPasswordCommandHandlerTests` | `ResetPasswordCommandHandler` | `UserManager<User>` | Success, invalid token, user not found |
| `ExternalLoginCommandHandlerTests` | `ExternalLoginCommandHandler` | `UserManager<User>`, `ITokenService`, `IMapper` | Success (existing user), success (new user from external), token invalid |
| `UpdateProfileCommandHandlerTests` | `UpdateProfileCommandHandler` | `UserManager<User>`, `IMapper` | Success, user not found |
| `ChangeAvatarCommandHandlerTests` | `ChangeAvatarCommandHandler` | `UserManager<User>`, `IFileService` | Success, user not found |
| `ChangePasswordCommandHandlerTests` | `ChangePasswordCommandHandler` | `UserManager<User>` | Success, user not found, incorrect current password |
| `AssignRolesCommandHandlerTests` | `AssignRolesCommandHandler` | `UserManager<User>` | Success, user not found |

**Query handler test classes**:

| Test Class | Handler Under Test | Mocked Dependencies | Scenarios |
|------------|-------------------|---------------------|-----------|
| `GetPaginatedProductsQueryHandlerTests` | `GetPaginatedProductsQueryHandler` | `IUnitOfWork`, `IMapper` | Success with results, empty result, pagination metadata |
| `GetProductByIdQueryHandlerTests` | `GetProductByIdQueryHandler` | `IUnitOfWork`, `IMapper` | Found, not found (NotFoundException) |
| `GetAllCategoriesQueryHandlerTests` | `GetAllCategoriesQueryHandler` | `IUnitOfWork`, `IMapper` | Success with ordered results, empty result |
| `GetCategoryByIdQueryHandlerTests` | `GetCategoryByIdQueryHandler` | `IUnitOfWork`, `IMapper` | Found, not found |
| `GetMyProfileQueryHandlerTests` | `GetMyProfileQueryHandler` | `UserManager<User>`, `IMapper` | Found, user not found |
| `GetPaginatedUsersQueryHandlerTests` | `GetPaginatedUsersQueryHandler` | `UserManager<User>`, `IMapper` | Success, empty result |
| `GetUserByIdQueryHandlerTests` | `GetUserByIdQueryHandler` | `UserManager<User>`, `IMapper` | Found, not found |
| `GetAllBannersQueryHandlerTests` | `GetAllBannersQueryHandler` | `IUnitOfWork`, `IMapper` | Success with results, empty result |
| `GetBannerByIdQueryHandlerTests` | `GetBannerByIdQueryHandler` | `IUnitOfWork`, `IMapper` | Found, not found |
| `GetVariantsByProductIdQueryHandlerTests` | `GetVariantsByProductIdQueryHandler` | `IUnitOfWork`, `IMapper` | Found, product not found |
| `GetVariantByIdQueryHandlerTests` | `GetVariantByIdQueryHandler` | `IUnitOfWork`, `IMapper` | Found, not found |

**AutoMapper profile test classes**:

| Test Class | Profiles Under Test |
|------------|---------------------|
| `ProductMapperTests` | `ProductMapper` — validates `Product→ProductDto`, `Product→ProductSummaryDto`, `ProductVariant→ProductVariantDto`, `ProductImage→ProductImageDto` |
| `CategoryMapperTests` | `CategoryMapper` — validates `Category→CategoryDto` |
| `UserMapperTests` | `UserMapper` — validates `User→UserDto` |
| `BannerMapperTests` | `BannerMapper` — validates `Banner→BannerDto` |

---

## Shared Test Helpers (Inline Static Methods)

No separate test utilities project. Shared helpers are inline `static` factory methods within each test class (no premature abstraction). Key patterns:

| Helper | Location | Purpose |
|--------|----------|---------|
| `CreateMockUserManager()` | `SignUpCommandHandlerTests` (copy to other Auth tests) | Construct `Mock<UserManager<User>>` with all 9 constructor parameters |
| `CreateValidSignUpCommand()` | `SignUpCommandHandlerTests` | Build a valid `SignUpCommand` for happy-path tests |
| `CreateValidProduct()` | `ProductAggregateTests` | Build a `Product` entity with all required fields for domain tests |
| `CreateValidCategory()` | `CategoryAggregateTests` | Build a `Category` entity for domain tests |

---

## No Database Changes

This feature adds zero new tables, migrations, or schema changes. All repository access is mocked via `Mock<IUnitOfWork>()`.
