# Research: Set Up Unit Tests

**Feature**: 008-unit-tests-setup  
**Date**: 2026-04-12  
**Status**: Complete — all decisions resolved

---

## Decision 1: Test Framework

**Decision**: xUnit 2.9.3

**Rationale**: xUnit is the de-facto standard for .NET OSS and ASP.NET Core projects. It is fully compatible with .NET 10, integrates natively with `dotnet test`, Visual Studio Test Explorer, and GitHub Actions. It requires no test lifecycle attributes (`[SetUp]`/`[TearDown]`), promoting constructor-based dependency injection in test classes — aligning with the project's clean architecture.

**Alternatives considered**:
- NUnit 4.x: Feature-equivalent but carries legacy `[SetUp]/[TearDown]` ceremony. No advantage over xUnit for this codebase.
- MSTest: Microsoft-maintained but less popular in the .NET OSS community. Verbose attribute syntax.

**Package versions**:
| Package | Version |
|---------|---------|
| `xunit` | 2.9.3 |
| `xunit.runner.visualstudio` | 3.1.5 |
| `Microsoft.NET.Test.Sdk` | 18.4.0 |

---

## Decision 2: Mocking Library

**Decision**: Moq 4.20.72

**Rationale**: Moq is the most widely adopted .NET mocking library. Fluent lambda-based API (`Setup().Returns()`, `Verify()`) integrates cleanly with the repository and service interfaces in this project. Full async support via `.ReturnsAsync()`. No source-generator dependency (unlike NSubstitute 5+). Explicitly supports .NET 10.

**Alternatives considered**:
- NSubstitute 5.x: Clean syntax but requires Roslyn source generators, adding build-time complexity.
- FakeItEasy: Smaller community, less documentation coverage for ASP.NET Core Identity patterns.

**Package**: `Moq` 4.20.72

---

## Decision 3: Assertion Library

**Decision**: FluentAssertions 8.9.0

**Rationale**: FluentAssertions produces human-readable failure messages (`expected X to be Y but found Z`), reducing debugging time. Version 8.9.0 is fully .NET 10 compatible. The v8 migration from v6/v7 introduced `AssertionChain` — no impact since this project starts fresh.

**Key v8 pattern** (start fresh, no migration needed):
```csharp
result.Should().NotBeNull();
result.Name.Should().Be("Espresso");
result.Should().BeOfType<ProductDto>();
exception.Should().BeOfType<NotFoundException>();
```

**Package**: `FluentAssertions` 8.9.0

---

## Decision 4: FluentValidation Test Pattern

**Decision**: Built-in `TestValidate()` / `TestValidateAsync()` from `FluentValidation` package — no extra package needed.

**Rationale**: FluentValidation 12.1.1 (already a project dependency) ships with `TestHelper` extensions. Using `validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.FieldName)` is the official recommended pattern. No separate package required.

**Pattern**:
```csharp
var validator = new CreateProductCommandValidator();
var result = validator.TestValidate(command);
result.ShouldHaveValidationErrorFor(x => x.Name);
result.ShouldNotHaveAnyValidationErrors();
```

**Gotcha**: `TestValidate()` is synchronous; use `TestValidateAsync()` for validators with async rules (e.g., database uniqueness checks via `MustAsync`). For the current validators (no async rules), `TestValidate()` is sufficient.

---

## Decision 5: AutoMapper Test Pattern

**Decision**: `MapperConfiguration.AssertConfigurationIsValid()` + direct `Mapper` instantiation (no DI).

**Rationale**: AutoMapper 16.1.1 (already a project dependency) supports isolated `MapperConfiguration` instances. `AssertConfigurationIsValid()` throws `AutoMapperConfigurationException` on misconfigured mappings — this is the canonical smoke test for profiles.

**Pattern**:
```csharp
var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductMapper>());
config.AssertConfigurationIsValid();   // smoke test
var mapper = config.CreateMapper();
var dto = mapper.Map<ProductDto>(product);
dto.Name.Should().Be(product.Name);
```

**Gotcha**: `Product → ProductDto` maps `ProductCategories` → `Categories` via a `Select(pc => pc.Category)`. In tests, the `ProductCategories` navigation property must be populated with entities that have the `Category` property set, or the mapping test will produce an empty list.

---

## Decision 6: MediatR Handler Test Pattern

**Decision**: Direct handler instantiation with constructor-injected mocks (unit test style — NOT through `IMediator.Send()`).

**Rationale**: Unit tests should test the handler in isolation. Testing through `IMediator.Send()` exercises the MediatR pipeline (behaviours, middleware) which is an integration concern. For unit tests, we instantiate the handler directly, inject mocks via constructor, and call `Handle()` directly. This is faster, more focused, and does not require DI setup.

**Pattern**:
```csharp
// Arrange
var mockUoW = new Mock<IUnitOfWork>();
var mockMapper = new Mock<IMapper>();
var handler = new GetAllCategoriesQueryHandler(mockUoW.Object, mockMapper.Object);

// Act
var result = await handler.Handle(query, CancellationToken.None);

// Assert
result.Should().NotBeNull();
mockUoW.Verify(uow => uow.Categories.FindAll(), Times.Once);
```

**Gotcha**: MediatR 14 does not require any test-specific packages. The handler's `Handle()` method is public and directly invokable.

---

## Decision 7: Mocking ASP.NET Core Identity UserManager

**Decision**: Mock `UserManager<User>` by constructing it with a mocked `IUserStore<User>` and all required constructor parameters.

**Rationale**: `UserManager<T>` is a concrete class with 9 constructor parameters. The simplest approach is to create a `Mock<IUserStore<User>>()` (required) and pass `null` or simple defaults for the rest. All async methods (`FindByEmailAsync`, `CreateAsync`, `AddToRoleAsync`) are virtual and can be Setup'd directly on the mock.

**Pattern**:
```csharp
private static Mock<UserManager<User>> CreateMockUserManager()
{
    var store = new Mock<IUserStore<User>>();
    store.As<IUserEmailStore<User>>();
    var mgr = new Mock<UserManager<User>>(
        store.Object,
        Mock.Of<IOptions<IdentityOptions>>(),
        Mock.Of<IPasswordHasher<User>>(),
        Array.Empty<IUserValidator<User>>(),
        Array.Empty<IPasswordValidator<User>>(),
        Mock.Of<ILookupNormalizer>(),
        new IdentityErrorDescriber(),
        Mock.Of<IServiceProvider>(),
        Mock.Of<ILogger<UserManager<User>>>()
    );
    return mgr;
}

// In test:
var userManager = CreateMockUserManager();
userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync((User?)null);
userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
           .ReturnsAsync(IdentityResult.Success);
```

**Gotcha**: All methods on `UserManager<T>` must be Setup'd explicitly — no automatic behavior. `FindByEmailAsync` returns `null` by default (Moq returns default(T) for reference types). `CreateAsync` returns `IdentityResult.Failed()` by default — always explicitly Setup the return value.

---

## Decision 8: Test Project Naming & TreatWarningsAsErrors

**Decision**: Two test projects: `MoriiCoffee.Domain.Tests` and `MoriiCoffee.Application.Tests`. Both target `net10.0` and inherit `TreatWarningsAsErrors=true` from `Directory.Build.props`.

**Rationale**: Matching production project naming conventions. Domain and Application tests are separate because they have different dependencies. `TreatWarningsAsErrors=true` is inherited — test projects must be warning-free.

**Gotcha**: `Directory.Build.props` sets `TreatWarningsAsErrors=true` globally. Test projects will inherit this. All code must compile warning-free. Use `#pragma warning disable` only for unavoidable cases (rare).

---

## Decision 9: Handler Testing for Commands That Use IFormFile

**Decision**: Use `Mock<IFormFile>()` for commands that accept file uploads (CreateProduct thumbnail, UploadProductImages, etc.).

**Rationale**: `IFormFile` is an ASP.NET Core interface. Moq can mock it directly. Tests that exercise the file upload path should mock `IFileService.UploadAsync()` and return a stub `BlobResponseDto`.

**Pattern**:
```csharp
var mockFile = new Mock<IFormFile>();
mockFile.Setup(f => f.FileName).Returns("product.jpg");
mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
mockFile.Setup(f => f.Length).Returns(1024);

var mockFileService = new Mock<IFileService>();
mockFileService
    .Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
    .ReturnsAsync(new BlobResponseDto { Uri = "https://storage/product.jpg", FileName = "product.jpg" });
```

---

## Summary Table

| Concern | Decision | Package | Version |
|---------|----------|---------|---------|
| Test framework | xUnit | `xunit` | 2.9.3 |
| Test runner (IDE) | xUnit Visual Studio runner | `xunit.runner.visualstudio` | 3.1.5 |
| Test SDK | Microsoft Test SDK | `Microsoft.NET.Test.Sdk` | 18.4.0 |
| Mocking | Moq | `Moq` | 4.20.72 |
| Assertions | FluentAssertions | `FluentAssertions` | 8.9.0 |
| Validator testing | FluentValidation built-in | (no extra package) | — |
| Mapper testing | AutoMapper built-in | (no extra package) | — |
| Handler testing | Direct instantiation | (no extra package) | — |
| UserManager mocking | Moq constructor | (no extra package) | — |
