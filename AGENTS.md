<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan:
`specs/018-vnpay-integration/plan.md`
<!-- SPECKIT END -->

## Active Technologies
- C# / .NET 10 (`net10.0`) across the backend projects + Existing stack only for backend implementation: ASP.NET Core Web API, MediatR, FluentValidation, AutoMapper, EF Core 10, Npgsql, Swashbuckle, AWS S3/MinIO file services. Frontend consumer is expected to use Tiptap + React Hook Form + Zod, but those stay outside this repo's implementation scope for now. (012-blog-management)
- PostgreSQL via EF Core + Npgsql; public asset storage via the existing S3/MinIO-backed file service (012-blog-management)
- C# / .NET 10 (`net10.0`) across backend projects + ASP.NET Core Web API, MediatR 14, FluentValidation 12, EF Core 10, Npgsql 10, existing Redis cache abstraction, existing Stripe.net integration, `HttpClient`, built-in cryptography APIs (018-vnpay-integration)
- PostgreSQL for orders, payment attempts, webhook/IPN audits, and refund history; existing cache service for provider-neutral checkout drafts (018-vnpay-integration)

## Recent Changes
- 012-blog-management: Added C# / .NET 10 (`net10.0`) across the backend projects + Existing stack only for backend implementation: ASP.NET Core Web API, MediatR, FluentValidation, AutoMapper, EF Core 10, Npgsql, Swashbuckle, AWS S3/MinIO file services. Frontend consumer is expected to use Tiptap + React Hook Form + Zod, but those stay outside this repo's implementation scope for now.
