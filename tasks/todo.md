# Stripe Payment Todo

## Completed

- Reviewed the Stripe payment implementation end-to-end instead of only trusting the stale checklist.
- Fixed the `Payment.Id` / Stripe metadata mismatch in checkout-session creation.
- Added forensic audit persistence for invalid-signature webhook requests.
- Replaced Stripe startup logging that used `BuildServiceProvider()` with a hosted diagnostics service.
- Strengthened tests for refund exhaustion and COD non-regression assertions.
- Wrote the required ENG/VN integration guides and ENG/VN summary documents.
- Updated the feature checklist to reflect implemented work through Phase 7 documentation tasks.

## Remaining verification

- Execute the quickstart E2E flow against a live local stack + Stripe CLI

## Verified in this session

- `dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental`
  Result: `0 errors, 0 warnings`
- `dotnet test source/MoriiCoffee.Application.Tests/MoriiCoffee.Application.Tests.csproj`
  Result: `268 tests passed`
- `dotnet test source/MoriiCoffee.Domain.Tests/MoriiCoffee.Domain.Tests.csproj`
  Result: `81 tests passed`
- `dotnet ef migrations script --idempotent`
  Result: confirmed `Orders.PaymentStatus int NOT NULL DEFAULT 1`, tables `Payments` / `Refunds` / `PaymentWebhookEvents`, and the expected Stripe-related indexes

## Current blocker

- T071 cannot be executed in the current machine state because `Stripe__SecretKey`, `Stripe__PublishableKey`, and `Stripe__WebhookSigningSecret` are unset, and the `stripe` CLI is not installed.

## Review summary

Highest-value fixes from this review:

- correctness:
  Stripe metadata now points at the real persisted `Payment` row
- security/forensics:
  invalid webhook signatures are now audited, not only rejected
- cleanliness:
  Stripe configuration no longer builds a temporary service provider during DI registration
- test quality:
  COD non-regression test now asserts something meaningful about payment persistence behavior
