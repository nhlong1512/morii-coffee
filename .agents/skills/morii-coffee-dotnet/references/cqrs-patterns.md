# CQRS Patterns — MoriiCoffee

## Command Structure (Write Operations)

Every command lives in its own folder: `Commands/{Aggregate}/{ActionName}/`

### File 1: Command record
```csharp
// Commands/Product/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; }
    public string Description { get; init; }
    public List<Guid> CategoryIds { get; init; } = [];
    // ... other fields
}
```

### File 2: Validator (FluentValidation)
```csharp
// Commands/Product/CreateProduct/CreateProductCommandValidator.cs
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Description)
            .NotEmpty();
    }
}
```

### File 3: Handler (business logic)
```csharp
// Commands/Product/CreateProduct/CreateProductCommandHandler.cs
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Description);
        // Apply domain logic here

        await _unitOfWork.Products.CreateAsync(product);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<ProductDto>(product);
    }
}
```

---

## Query Structure (Read Operations)

Every query lives in its own folder: `Queries/{Aggregate}/{ActionName}/`

### Paginated query example
```csharp
// Queries/Product/GetPaginatedProducts/GetPaginatedProductsQuery.cs
public record GetPaginatedProductsQuery : IRequest<Pagination<ProductSummaryDto>>
{
    public ProductPaginationFilter Filter { get; init; } = new();
}

// Handler
public class GetPaginatedProductsQueryHandler
    : IRequestHandler<GetPaginatedProductsQuery, Pagination<ProductSummaryDto>>
{
    public async Task<Pagination<ProductSummaryDto>> Handle(
        GetPaginatedProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _unitOfWork.Products.GetPaginatedAsync(request.Filter);
        return _mapper.Map<Pagination<ProductSummaryDto>>(products);
    }
}
```

### Single entity query example
```csharp
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} not found");

        return _mapper.Map<ProductDto>(product);
    }
}
```

---

## MediatR Pipeline Behaviors

Register in `Application` layer DI setup. Standard pipeline:

```
Request → ValidationBehavior → LoggingBehavior → Handler
```

### Validation behavior
```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## DTO Conventions

- **`ProductDto`** — full detail (returned by GetById, Create, Update)
- **`ProductSummaryDto`** — lightweight for list responses (name, id, status, thumbnail)
- **`CreateProductDto`** — input from API controller for create
- **`UpdateProductDto`** — input from API controller for update

Map input DTOs → Commands in the controller using AutoMapper. Map entities → output DTOs in the handler.

---

## AutoMapper Profile
```csharp
// SeedWork/Mappings/ProductMapper.cs
public class ProductMapper : Profile
{
    public ProductMapper()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Categories,
                opt => opt.MapFrom(src => src.ProductCategories.Select(pc => pc.Category)));

        CreateMap<Product, ProductSummaryDto>();
        CreateMap<CreateProductDto, CreateProductCommand>();
        CreateMap<UpdateProductDto, UpdateProductCommand>();
    }
}
```

---

## Registration in DI (Application layer)
```csharp
// In InfrastructureServiceExtensions or ApplicationServiceExtensions
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(CreateProductCommandHandler).Assembly));

services.AddValidatorsFromAssembly(typeof(CreateProductCommandValidator).Assembly);

services.AddAutoMapper(typeof(ProductMapper).Assembly);

// Register pipeline behaviors
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```
