# Quickstart: Running Unit Tests

**Feature**: 008-unit-tests-setup  
**Date**: 2026-04-12

---

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` → `10.x.x`)
- No external services required (no database, no Docker, no network)

---

## Run All Unit Tests

From the repository root:

```sh
dotnet test source/
```

Or target a specific test project:

```sh
# Domain layer tests only
dotnet test source/MoriiCoffee.Domain.Tests/

# Application layer tests only
dotnet test source/MoriiCoffee.Application.Tests/
```

---

## Run with Verbose Output

```sh
dotnet test source/ --logger "console;verbosity=detailed"
```

---

## Run a Specific Test Class or Method

```sh
# Run all tests in a specific class
dotnet test source/ --filter "FullyQualifiedName~SignUpCommandHandlerTests"

# Run a single test method
dotnet test source/ --filter "FullyQualifiedName=MoriiCoffee.Application.Tests.Commands.Auth.SignUpCommandHandlerTests.Handle_WhenEmailAlreadyExists_ThrowsBadRequestException"
```

---

## Run with Coverage (Optional)

To collect test coverage (requires `dotnet-coverage` global tool):

```sh
dotnet tool install --global dotnet-coverage
dotnet-coverage collect "dotnet test source/" -f xml -o coverage.xml
```

---

## Adding New Tests

1. Identify the layer: Domain entity → `MoriiCoffee.Domain.Tests`; handler/validator/mapper → `MoriiCoffee.Application.Tests`
2. Create a new test file mirroring the production project path:
   - Production: `MoriiCoffee.Application/Commands/Product/CreateProduct/CreateProductCommandHandler.cs`
   - Test: `MoriiCoffee.Application.Tests/Commands/Product/CreateProductCommandHandlerTests.cs`
3. Inject mock dependencies via the test class constructor (xUnit creates a new instance per test method)
4. Follow the Arrange/Act/Assert pattern

**Example structure**:

```csharp
namespace MoriiCoffee.Application.Tests.Commands.Product;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUoW;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockUoW = new Mock<IUnitOfWork>();
        _mockFileService = new Mock<IFileService>();
        _mockMapper = new Mock<IMapper>();
        _handler = new CreateProductCommandHandler(
            _mockUoW.Object, _mockFileService.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProductAndCommits()
    {
        // Arrange
        // ... setup mocks ...

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockUoW.Verify(uow => uow.CommitAsync(), Times.Once);
    }
}
```

---

## Solution Integration

Both test projects are registered in `MoriiCoffee.slnx`. Run from the IDE using the built-in Test Explorer (Visual Studio / Rider), or from the terminal using `dotnet test`.
