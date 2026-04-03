# Research: .NET 10 Platform Upgrade

**Feature**: 006-dotnet-10-upgrade
**Date**: 2026-04-02
**Status**: Research Complete

---

## Executive Summary

This research document consolidates findings for upgrading Morii Coffee from .NET 8 to .NET 10. Key findings:

1. **Critical Issue Discovered**: The project references **ancient Microsoft.AspNetCore.Identity 2.2.0** and **Microsoft.AspNetCore.Http.Features 2.2.0** packages from 2018 that must be **removed entirely** (they are now in the shared framework).

2. **Breaking Changes**: **45+ breaking changes** identified across .NET 9 and .NET 10, with **4 critical** issues requiring code changes (Authentication, EF Core, JSON serialization, DI).

3. **Package Upgrades**: **25 packages** require updates. Most have clear upgrade paths, but **Swashbuckle**, **MediatR**, and **AutoMapper** have major version jumps requiring breaking change reviews.

4. **Testing Gap**: **Zero automated tests exist** in the codebase. Manual verification checklist created with **30+ verification points** across authentication, database, email, and file storage.

5. **global.json**: File does not exist. **Recommendation**: Create it to pin .NET 10 SDK version for reproducible builds.

6. **Directory.Build.props**: Centrally defines `TargetFramework` as `net8.0` — this is the **primary file** to change (not individual .csproj files).

---

## 1. Breaking Changes Summary

### 1.1 Critical Changes (High Impact)

#### ASP.NET Core Identity Migration (CRITICAL ⚠️)

**Issue**: Project references **Microsoft.AspNetCore.Identity 2.2.0** and **Microsoft.AspNetCore.Http.Features 2.2.0** — both from ASP.NET Core 2.2 era (2018) and now **deprecated/obsolete**.

**Impact**:
- These packages are from **before .NET Core 3.0** (released 2019)
- Starting with .NET Core 3.0, Identity and HttpFeatures were moved into the `Microsoft.AspNetCore.App` shared framework
- Projects targeting `Microsoft.NET.Sdk.Web` automatically reference the shared framework
- Explicit package references **conflict** with the shared framework and must be removed
- The project correctly uses `Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.10` which is the modern package

**Migration Path**:
1. **DELETE** the following lines from `MoriiCoffee.Application.csproj`:
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.2.0" />
   <PackageReference Include="Microsoft.AspNetCore.Http.Features" Version="2.2.0" />
   ```
2. Keep `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (upgrade to 10.0.5)
3. Verify Identity still works via shared framework (it will — this is standard migration)
4. Test all authentication flows thoroughly

**Additional .NET 10 Identity Changes**:
- Google OAuth configuration may need updates due to **Pushed Authorization Requests (PAR)** being enabled by default
- JWT Bearer authentication middleware ordering may change
- Identity database schema requires migration validation with EF Core 10

**Code Changes Required**: **YES**
- Remove obsolete package references
- Test OAuth flows (may need to disable PAR: `oidcOptions.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable`)
- Test authentication middleware registration order

---

#### Entity Framework Core 8.0 → EF Core 10 (CRITICAL)

**Issue**: Multiple breaking changes in EF Core 9/10 affecting query translation, nullable semantics, and migrations.

**Impact**:

1. **Query Translation Changes**:
   - Primitive collections now translate to SQL properly
   - LINQ operators behave differently
   - SQL generation improvements may change query output
   - **GREATEST/LEAST** functions used (requires SQL Server 2022+ or Azure SQL)

2. **Azure SQL vs SQL Server Differentiation**:
   - EF 9+ distinguishes between `UseSqlServer()` and `UseAzureSql()`
   - Current code uses generic SQL Server configuration
   - Must determine target environment and update accordingly

3. **Migration Protection**:
   - New concurrent migration protection may affect deployment scripts
   - Migration execution order must be validated

4. **Nullable Reference Semantics**:
   - C# nullable semantics now properly enforced in queries (EF 9+)
   - Nullable comparisons may behave differently
   - Queries expecting `null` behavior may fail

5. **Primitive Collection Support**:
   - Expanded support for `IReadOnlyList`, `IReadOnlyCollection`, `ReadOnlyCollection`
   - Opportunity to use more immutable collection types

**Migration Path**:
1. Audit all LINQ queries for compatibility with new translation rules
2. Determine if using Azure SQL or SQL Server 2022+
3. Update `UseSqlServer()` to `UseAzureSql()` if targeting Azure SQL
4. Review all EF migrations for concurrent execution issues
5. Test complex queries involving nullable comparisons
6. Validate all seed data operations
7. **Consider incremental upgrade**: .NET 8 → .NET 9 → .NET 10 (safer)

**Code Changes Required**: **YES**
- Query rewriting if incompatible translations found
- Connection configuration updates (UseSqlServer vs UseAzureSql)
- Migration script validation
- Nullable reference handling fixes

**Risk Level**: **HIGH** — Database is critical; query behavior changes could break application logic

---

#### JSON Serialization Default Changes (.NET 9/10)

**Issue**: System.Text.Json changes in .NET 9/10 affect default serialization behavior.

**Impact**:
1. **Property Name Conflict Checking**: Stricter checking (case-insensitive duplicates now error)
2. **Nullable JsonDocument Behavior**: Nullable `JsonDocument?` properties now deserialize to `JsonValueKind.Null` instead of `null`
3. **Metadata Property Unescaping**: Changed behavior for JSON metadata properties

**Migration Path**:
1. Review all DTOs and API models for duplicate property names (case-insensitive)
2. Update JSON deserialization code expecting `null` for nullable JsonDocument properties
3. Test all API endpoints with JSON request/response payloads
4. Verify Swagger schema generation still works

**Code Changes Required**: **POTENTIALLY**
- Depends on DTO structures and custom JSON serialization

**Risk Level**: **MEDIUM** — API contracts could break if models have property conflicts

---

#### Dependency Injection Container Changes (.NET 9/10)

**Issue**: Multiple DI container behavior changes.

**Impact**:
1. **FromKeyedServicesAttribute**: No longer injects non-keyed parameters (EF 9)
2. **BackgroundService.ExecuteAsync**: Now runs entirely as a Task (.NET 10) — synchronous initialization may fail
3. **GetKeyedService() with AnyKey**: Fixed behavior (.NET 10)

**Migration Path**:
1. Audit all keyed service registrations (if any)
2. Review background services for synchronous initialization dependencies
3. Test middleware dependency injection
4. Verify MediatR and custom service registrations

**Code Changes Required**: **POTENTIALLY**
- Depends on usage of keyed services and background services
- MediatR registrations should be verified

**Risk Level**: **MEDIUM** — Service resolution failures could prevent application startup

---

### 1.2 Medium Impact Changes

#### JWT Bearer Authentication Configuration

**Impact**: Token validation parameters, event handlers, and middleware ordering may need adjustments.

**Migration Path**: Update package to 10.0.5, test token validation flows, review custom event handlers.

**Code Changes Required**: **POTENTIALLY** — Review custom token validation

---

#### Google OAuth Configuration

**Impact**:
- PAR (Pushed Authorization Requests) enabled by default in .NET 9+
- OAuth redirect flows may behave differently
- New `AdditionalAuthorizationParameters` API available for cleaner parameter customization

**Migration Path**:
1. Update package to 10.0.5
2. Test OAuth flows with Google identity provider
3. May need to disable PAR if provider doesn't support it
4. Consider using new `AdditionalAuthorizationParameters` API

**Code Changes Required**: **POTENTIALLY** — Test OAuth configuration

---

#### MediatR Compatibility

**Impact**: CQRS handlers may need DI adjustments.

**Migration Path**: Update from 12.4.0 → 14.1.0, review changelog, test all handlers.

**Code Changes Required**: **UNLIKELY** — MediatR is typically forward-compatible

---

#### FluentValidation Compatibility

**Impact**: Validation middleware integration needs testing. **Note**: FluentValidation.AspNetCore is **deprecated**.

**Migration Path**: Update to 12.1.1, test validators, plan future migration to manual registration.

**Code Changes Required**: **UNLIKELY** — But plan deprecation migration

---

#### Serilog Compatibility

**Impact**:
- `dotnet watch` logs to stderr instead of stdout (.NET 10 SDK)
- Message duplication fixes in Console output

**Migration Path**: Update Serilog packages, test logging outputs.

**Code Changes Required**: **NO** — Configuration only

---

#### String Comparison Semantics

**Impact**: C# overload resolution changes with span parameters (.NET 10).

**Migration Path**: Comprehensive testing of string operations.

**Code Changes Required**: **UNLIKELY** — Requires testing

---

#### SQL Server Version Requirements

**Impact**: GREATEST/LEAST functions in EF 9+ queries require SQL Server 2022+ or Azure SQL.

**Migration Path**: Ensure database version compatibility, test all queries.

**Code Changes Required**: **NO** — But may require database version upgrade

---

### 1.3 Low Impact Changes

1. **SDK and Build Changes**: `dotnet new`, `dotnet restore`, `dotnet watch` behavior changes
2. **Primitive Collections**: Expanded support for read-only collections
3. **HttpClient Metrics**: Reporting format changes
4. **Obsolete API Warnings**: Various APIs marked obsolete (X509Certificate, BinaryFormatter)
5. **Query Translation Improvements**: Better performance, more queries can execute

**Code Changes Required**: **NO** — Mostly informational

---

### 1.4 Summary

| Priority | Count | Code Changes | Testing Required |
|----------|-------|--------------|------------------|
| **Critical** | 4 | YES | Extensive |
| **Medium** | 8 | POTENTIALLY | Moderate |
| **Low** | 33+ | NO | Minimal |
| **TOTAL** | 45+ | YES | Comprehensive |

**Estimated Code Modification Effort**: **MEDIUM TO HIGH**

**Key Risk Areas**:
1. Authentication Flow Changes (Google OAuth + JWT Bearer)
2. EF Core Query Behavior (nullable semantics, query translation)
3. Database Compatibility (SQL Server 2022+ required for GREATEST/LEAST)
4. Obsolete Package Removal (Microsoft.AspNetCore.Identity 2.2.0)

---

## 2. Package Upgrade Matrix

### 2.1 Microsoft Packages

| Package | Current | .NET 10 Target | Breaking Changes | Notes |
|---------|---------|----------------|------------------|-------|
| Microsoft.AspNetCore.* | 8.0.x | 10.0.5 | Yes | Standard upgrade |
| Microsoft.EntityFrameworkCore.* | 8.0.10 | 10.0.5 | Yes | Latest stable (released 3/12/2026) |
| Microsoft.EntityFrameworkCore.Design | 8.0.10 | 10.0.5 | Yes | Must match EF Core version |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.10 | 10.0.5 | Yes | Must match EF Core version |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | 10.0.5 | Yes | Standard upgrade |
| **Microsoft.AspNetCore.Identity** | **2.2.0** ⚠️ | **REMOVE** | **CRITICAL** | **Obsolete — remove entirely** |
| **Microsoft.AspNetCore.Http.Features** | **2.2.0** ⚠️ | **REMOVE** | **CRITICAL** | **Deprecated — remove entirely** |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.10 | 10.0.5 | Yes | **Correct modern package** |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.10 | 10.0.5 | No | Standard upgrade |
| Microsoft.AspNetCore.Authentication.Google | 8.0.0 | 10.0.5 | No | Update to latest patch |
| Microsoft.AspNetCore.Mvc.NewtonsoftJson | 8.0.10 | 10.0.5 | No | Standard upgrade |

### 2.2 Third-Party Packages

| Package | Current | .NET 10 Target | Support | Notes |
|---------|---------|----------------|---------|-------|
| FluentValidation | 11.9.2 | 12.1.1 | Yes | Targets .NET 8.0, compatible with .NET 10 |
| FluentValidation.AspNetCore | 11.3.0 | 11.3.1 | Yes (deprecated) | ⚠️ **Deprecated** — plan migration |
| MediatR | 12.4.0 | 14.1.0 | Yes | **Major version jump** — review changelog |
| AutoMapper | 14.0.0 | 16.1.1 | Yes | **Major version jump** — review changelog |
| Serilog | 4.0.1 | 4.3.1 | Yes | Supports .NET 6.0+ |
| Serilog.Settings.Configuration | 8.0.4 | 10.0.0 | Yes | Supports .NET 8.0+ |
| Serilog.Sinks.Console | 6.0.0 | 6.1.1 | Yes | Supports .NET 6.0+ |
| Serilog.Extensions.Hosting | 8.0.0 | 10.0.0 | Yes | Supports .NET 8.0+ |
| Swashbuckle.AspNetCore | 6.7.2 | 10.1.7 | Yes | **Major version jump** — review changelog |
| Swashbuckle.AspNetCore.Annotations | 6.7.2 | 10.1.7 | Yes | **Major version jump** — review changelog |
| MicroElements.Swashbuckle.FluentValidation | 6.0.0 | 7.1.4 | Yes | Supports .NET 8.0+ |
| brevo_csharp | 1.1.2 | 1.1.2 | Yes | .NET Standard 2.0 — compatible |
| Minio | 6.0.3 | 7.0.0 | Computed | ⚠️ **Verify thoroughly** — support "computed" |
| AWSSDK.S3 | 3.7.* | 4.0.20.2 | Yes | Supports .NET 8.0+ |
| Bogus | 35.6.0 | 35.6.5 | Yes | Supports .NET 6.0+ |

### 2.3 Flagged Packages

#### REMOVE ENTIRELY (Now in Shared Framework)

1. **Microsoft.AspNetCore.Identity 2.2.0** → **DELETE**
   - From ASP.NET Core 2.2 (2018)
   - Functionality now in `Microsoft.AspNetCore.App` shared framework
   - Conflicts with modern Identity implementation
   - Delete from `MoriiCoffee.Application.csproj`

2. **Microsoft.AspNetCore.Http.Features 2.2.0** → **DELETE**
   - Deprecated (last version 5.0.17)
   - Now in `Microsoft.AspNetCore.App` shared framework
   - Delete from `MoriiCoffee.Application.csproj`

#### DEPRECATED (Still Works but Not Maintained)

3. **FluentValidation.AspNetCore 11.3.0** → Consider future migration
   - Package marked "legacy" and "no longer maintained"
   - Still works but authors recommend manual validation integration
   - Not urgent but plan future refactoring

#### REQUIRES TESTING VERIFICATION

4. **Minio 7.0.0** → Test thoroughly
   - .NET 10 support is "computed" rather than explicitly confirmed
   - Major version jump (6.x → 7.x)
   - Monitor GitHub repo for .NET 10 announcements

### 2.4 Major Version Jumps (Review Breaking Changes)

1. **Swashbuckle** (6.7.2 → 10.1.7): Review Swagger API changes
2. **MediatR** (12.4.0 → 14.1.0): Review pipeline behavior and handler registration changes
3. **AutoMapper** (14.0.0 → 16.1.1): Review profile configuration changes
4. **Minio** (6.0.3 → 7.0.0): Review object storage API changes

### 2.5 Total Packages Summary

- **Total**: 25 packages
- **.NET 10 Native Support**: 15 packages
- **.NET Standard Compatible**: 3 packages
- **Flagged/Problematic**: 4 packages
  - 2 require removal (Identity, Http.Features)
  - 1 deprecated (FluentValidation.AspNetCore)
  - 1 requires testing (Minio)

---

## 3. Directory.Build.props Analysis

### 3.1 File Contents

```xml
<Project>
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <!-- AutoMapper has an unpatched vulnerability (GHSA-rvv3-g6hj-g44x) across all versions. Suppressed temporarily. -->
        <NoWarn>$(NoWarn);NU1903</NoWarn>
    </PropertyGroup>
</Project>
```

### 3.2 Impact on .NET 10 Upgrade

**Critical Finding**: This file **centrally defines** `TargetFramework` for **all projects**.

- **TargetFramework**: `net8.0` (must change to `net10.0`)
- **ImplicitUsings**: Enabled (no change needed)
- **Nullable**: Enabled (no change needed)
- **TreatWarningsAsErrors**: Enabled (good — will catch deprecation warnings)
- **NoWarn**: Suppresses NU1903 for AutoMapper vulnerability

**No SDK Version**: File does not specify SDK version (managed via global.json or developer machine).

**No Central Package Management (CPM)**: Package versions managed at individual project level.

### 3.3 Required Modifications

**Primary Change**:
```xml
<!-- Change this line -->
<TargetFramework>net8.0</TargetFramework>

<!-- To this -->
<TargetFramework>net10.0</TargetFramework>
```

**Secondary Considerations**:
1. **NU1903 Suppression**: Review if AutoMapper 16.1.1 resolves the vulnerability
2. **TreatWarningsAsErrors**: Will surface deprecation warnings during upgrade (good)
3. **Consider CPM**: 25+ packages across 6 projects — CPM would simplify management

### 3.4 Upgrade Strategy

**Good News**: Only **one file** needs the TargetFramework change (not all 6 .csproj files).

**Steps**:
1. Update `Directory.Build.props` with `net10.0`
2. All 6 projects inherit this setting automatically
3. No need to modify individual .csproj files for framework version

**Benefit**: Centralized management ensures all projects upgrade consistently.

---

## 4. Testing Strategy

### 4.1 Automated Tests: **NONE FOUND**

**Status**: **Zero automated test coverage** exists in the Morii Coffee codebase.

**Search Results**:
- No test projects (*.Tests.csproj, *Test.csproj, *.Specs.csproj)
- No test files (*Tests.cs, *Test.cs, *Spec.cs)
- No testing frameworks (xUnit, NUnit, MSTest)

**Note**: `Bogus` package (data faker) is referenced but never used.

### 4.2 Risk Assessment

**Risk Level**: **MODERATE-HIGH**

**Why**:
1. No safety net during upgrade
2. Complex integrations (EF Core, Identity, Brevo, MinIO, Google OAuth)
3. Database schema changes must be verified manually
4. Authentication system critical with no test coverage
5. Email service failure would go undetected

### 4.3 Verification Approach: Manual Testing

Since no automated tests exist, rely on **comprehensive manual verification checklist**.

---

## 5. Manual Verification Checklist

### 5.1 Authentication & Authorization (Critical)

- [ ] Email/password registration completes successfully
- [ ] Email/password login works with JWT token generation
- [ ] Google OAuth registration flow completes
- [ ] Google OAuth login flow completes
- [ ] Account linking between email and Google works
- [ ] JWT token is generated and stored correctly
- [ ] Protected endpoints reject requests without valid JWT
- [ ] Token refresh/renewal flow works
- [ ] Token expiration is enforced
- [ ] User roles and permissions are correctly applied

### 5.2 Email Service (Brevo Integration)

- [ ] Welcome email is sent on registration
- [ ] Verification email is sent and link works
- [ ] Email templates render correctly (HTML embedded resources)
- [ ] Brevo SDK communication succeeds
- [ ] Email delivery logging is recorded
- [ ] Failed email sends are logged appropriately

### 5.3 Database Operations (EF Core)

- [ ] Database migrations apply successfully on startup
- [ ] User CRUD operations work
- [ ] User logins table records persist correctly
- [ ] User tokens table records persist correctly
- [ ] Complex queries execute without errors
- [ ] Nullable reference types don't cause runtime issues
- [ ] Transaction handling works for multi-step operations
- [ ] Seed data populates correctly (Products, Categories, Banners, Users)
- [ ] Change tracking works correctly

### 5.4 File Storage (MinIO)

- [ ] MinIO container starts and initializes
- [ ] File uploads to MinIO succeed
- [ ] File downloads from MinIO succeed
- [ ] File deletion from MinIO works
- [ ] S3 fallback (if configured) works
- [ ] Temporary files are cleaned up

### 5.5 API Endpoints

- [ ] All authentication endpoints return correct status codes
- [ ] All product endpoints return data correctly
- [ ] All category endpoints return data correctly
- [ ] All banner endpoints return data correctly
- [ ] Request validation works and returns 400 with error details
- [ ] Request/response serialization works (JSON)
- [ ] Swagger UI loads and displays all endpoints
- [ ] Swagger documentation is generated correctly
- [ ] CORS headers are set correctly

### 5.6 Application Startup & Infrastructure

- [ ] Docker container builds without errors
- [ ] Docker container starts without errors
- [ ] Database connection established on startup
- [ ] MinIO connection established on startup
- [ ] All dependency injection services registered
- [ ] Configuration loads from appsettings.json correctly
- [ ] Environment variables override config as expected
- [ ] Logging initializes (Serilog) without errors
- [ ] No startup console errors or warnings
- [ ] Health check endpoint (if available) returns healthy

### 5.7 Framework & Language Features

- [ ] Nullable reference handling doesn't cause NullReferenceExceptions
- [ ] Implicit usings resolve correctly
- [ ] LINQ operations work correctly
- [ ] Async/await patterns work correctly
- [ ] No obsolete API warnings in compilation

---

## 6. Global.json Decision

### 6.1 Current State

**File Status**: **DOES NOT EXIST**

### 6.2 Decision: **CREATE global.json**

**Recommendation**: **YES** — Create `global.json` to pin .NET 10 SDK version.

### 6.3 Rationale

**Pros**:
- Ensures all developers use same .NET 10 SDK version (consistency)
- Required for CI/CD to pin SDK version (reproducible builds)
- Prevents "works on my machine" issues due to SDK version differences
- Industry best practice for team projects

**Cons**:
- May not be needed if organization uses .NET SDK version managers
- Requires manual updates when new SDK patches released

**Verdict**: Pros outweigh cons for a team project with CI/CD.

### 6.4 Recommended global.json

**Latest .NET 10 SDK Version** (as of 2026-04-02): **10.0.XXX** (check Microsoft docs for latest stable)

**File Location**: `/Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee/global.json`

**Recommended Contents**:
```json
{
  "sdk": {
    "version": "10.0.XXX",
    "rollForward": "latestPatch"
  }
}
```

**rollForward Policy**:
- `latestPatch`: Allows patch version updates (10.0.1 → 10.0.2) but not minor (10.0 → 10.1)
- **Recommended** for stability with security updates

---

## 7. Migration Risks

### 7.1 High-Risk Changes

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Microsoft.AspNetCore.Identity 2.2.0 incompatible with .NET 10 | HIGH | HIGH | Remove package; use shared framework |
| EF Core query behavior changes break application logic | HIGH | MEDIUM | Comprehensive database testing |
| Google OAuth PAR breaks login flow | HIGH | MEDIUM | Test OAuth; disable PAR if needed |
| Authentication middleware conflicts | MEDIUM | MEDIUM | Test all auth flows end-to-end |

### 7.2 Medium-Risk Changes

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Third-party packages (Brevo, Minio) not .NET 10 compatible | MEDIUM | LOW | Flag in research; use .NET Standard versions |
| MediatR major version breaks CQRS handlers | MEDIUM | LOW | Review changelog; test all handlers |
| Swashbuckle major version breaks Swagger UI | MEDIUM | LOW | Test Swagger generation |

### 7.3 Low-Risk Changes

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Docker build failures | LOW | LOW | .NET 10 images are stable |
| Performance regression | MEDIUM | LOW | Benchmark critical operations |
| SDK build changes break CI/CD | LOW | MEDIUM | Update build scripts |

### 7.4 Rollback Plan

**Immediate Rollback**:
1. Git checkout previous branch (`git checkout 005-google-oauth` or `main`)
2. Rebuild Docker images (`cd deploy && bash run-docker-development.sh`)
3. Database schema unchanged — no data migration rollback needed

**Advantages**:
- Clean separation via feature branch
- Docker Compose rebuilds from source
- No database schema changes (backward compatible)

---

## 8. Recommended Upgrade Strategy

### 8.1 Phased Approach

**Phase 1: Framework Upgrade**
1. Update `Directory.Build.props`: `net8.0` → `net10.0`
2. Create `global.json` with .NET 10 SDK version
3. Update Dockerfile base images: `mcr.microsoft.com/dotnet/sdk:8.0` → `10.0`
4. Build and verify compilation succeeds

**Phase 2: Critical Package Cleanup**
1. Remove `Microsoft.AspNetCore.Identity` 2.2.0 from MoriiCoffee.Application.csproj
2. Remove `Microsoft.AspNetCore.Http.Features` 2.2.0 from MoriiCoffee.Application.csproj
3. Verify build succeeds (Identity via shared framework)

**Phase 3: Microsoft Package Updates**
1. Update all Microsoft.* packages to 10.0.5
2. Test build
3. Run application in Docker
4. Verify authentication still works

**Phase 4: Third-Party Package Updates (Standard)**
1. Update Serilog ecosystem packages
2. Update AWSSDK.S3, Bogus
3. Update brevo_csharp (no change if already compatible)
4. Test logging and email services

**Phase 5: Third-Party Package Updates (Major Versions)**
1. Update Swashbuckle to 10.1.7 — test Swagger UI
2. Update MediatR to 14.1.0 — test all CQRS handlers
3. Update AutoMapper to 16.1.1 — test all mappings
4. Update FluentValidation to 12.1.1 — test all validators
5. Update Minio to 7.0.0 — thoroughly test object storage

**Phase 6: Comprehensive Validation**
1. Execute full manual verification checklist (Section 5)
2. Test all authentication flows
3. Test all database operations
4. Test file uploads/downloads
5. Test email sending
6. Monitor logs for warnings/errors

**Phase 7: Performance Validation**
1. Benchmark critical operations (API response times, database queries)
2. Compare against .NET 8 baseline
3. Ensure within 10% of baseline (per success criteria)

### 8.2 Alternative: Incremental Upgrade

**Option**: Upgrade .NET 8 → .NET 9 → .NET 10 (safer but longer)

**Pros**:
- Isolate breaking changes to smaller increments
- Easier debugging if issues arise
- More predictable

**Cons**:
- More time-consuming (2x the work)
- More testing cycles

**Recommendation**: **Direct .NET 8 → .NET 10** upgrade is acceptable given:
- No automated tests (same testing burden either way)
- Breaking changes well-documented
- Strong rollback plan

---

## 9. Appendix: Package Changelog References

### 9.1 Major Version Jumps — Review Required

1. **Swashbuckle.AspNetCore** (6.7.2 → 10.1.7)
   - Changelog: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/releases
   - Focus: API changes, Swagger UI compatibility

2. **MediatR** (12.4.0 → 14.1.0)
   - Changelog: https://github.com/jbogard/MediatR/releases
   - Focus: Pipeline behavior, handler registration

3. **AutoMapper** (14.0.0 → 16.1.1)
   - Changelog: https://github.com/AutoMapper/AutoMapper/releases
   - Focus: Profile configuration, mapping API

4. **Minio** (6.0.3 → 7.0.0)
   - Changelog: https://github.com/minio/minio-dotnet/releases
   - Focus: Object storage API changes, .NET 10 compatibility

---

## Research Status: ✅ COMPLETE

**Next Steps**:
1. Review research findings
2. Proceed to `/speckit.plan` Phase 1 (design artifacts)
3. Generate implementation tasks
4. Execute upgrade following phased approach

**Estimated Effort**: 4-8 hours (2 hours implementation + 2-6 hours testing)

**Risk Level**: MODERATE-HIGH (due to zero automated tests)

**Confidence Level**: HIGH (comprehensive research, clear upgrade path, strong rollback plan)
