# .NET 10 Quickstart Guide

**Feature**: 006-dotnet-10-upgrade
**Date**: 2026-04-02

---

## Prerequisites

### Install .NET 10 SDK

#### macOS/Linux

```bash
# Using official installer
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 10.0

# Or using Homebrew (macOS)
brew install dotnet@10
```

#### Windows

Download from: https://dotnet.microsoft.com/download/dotnet/10.0

#### Verify Installation

```bash
dotnet --version
# Expected: 10.0.102 (check global.json for required version)
```

### Docker

Ensure Docker Desktop or Docker Engine is installed and running. Any recent version supports .NET 10 base images.

```bash
docker --version
# Expected: 20.0.0 or higher
```

---

## Building the Project

### Local Build (without Docker)

```bash
# Navigate to solution root
cd /Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee

# Restore NuGet packages
dotnet restore

# Build all projects
dotnet build

# Verify build success
echo $?
# Expected: 0 (success)

# Run presentation layer locally
cd source/MoriiCoffee.Presentation
dotnet run
```

**Expected Output**:
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Access API**: http://localhost:5000/swagger

---

### Docker Build (Development Environment)

```bash
# Navigate to deployment directory
cd /Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee/deploy

# Run development environment (includes database, MinIO, API)
bash run-docker-development.sh
```

**What This Does**:
1. Builds .NET 10 Docker image using `source/MoriiCoffee.Presentation/Dockerfile`
2. Starts SQL Server container (moriicoffee.database)
3. Starts MinIO container (moriicoffee.minio)
4. Starts API container with hot reload (`dotnet watch`) (moriicoffee.api)
5. Exposes API at `http://localhost:8002`

**Expected Output**:
```
[+] Building 12.3s (XX/XX) FINISHED
 => [internal] load build definition from Dockerfile
 => => transferring dockerfile: XXXXb
 ...
 => exporting to image
 => => writing image sha256:...
 => => naming to docker.io/library/moriicoffee.api

[+] Running 3/3
 ✔ Container moriicoffee.database  Healthy
 ✔ Container moriicoffee.minio     Started
 ✔ Container moriicoffee.api       Started
```

**Access API**: http://localhost:8002/swagger

**Access MinIO Console**: http://localhost:9001 (credentials in docker-compose.yml)

---

## Verifying the Upgrade

### 1. Check SDK Version

```bash
dotnet --version
# Expected: 10.0.102 (matches global.json)
```

### 2. Verify global.json

```bash
cat global.json
# Expected:
# {
#   "sdk": {
#     "version": "10.0.102",
#     "rollForward": "latestPatch"
#   }
# }
```

### 3. Verify Project Target Frameworks

```bash
# Check Directory.Build.props for global setting
cat Directory.Build.props | grep TargetFramework
# Expected: <TargetFramework>net10.0</TargetFramework>

# Verify all projects inherit net10.0
dotnet build --no-restore -v minimal 2>&1 | grep "TargetFramework"
# Should show net10.0 for all projects
```

### 4. Verify Obsolete Packages Removed

```bash
# Check MoriiCoffee.Application.csproj for obsolete packages
grep -E "Microsoft.AspNetCore.Identity|Microsoft.AspNetCore.Http.Features" source/MoriiCoffee.Application/MoriiCoffee.Application.csproj
# Expected: No matches (packages removed)
```

### 5. Verify Package Versions

```bash
# Check Microsoft packages are .NET 10 versions (10.0.5)
grep -r "Microsoft\." source/**/*.csproj | grep PackageReference | grep -v "10.0"
# Expected: Minimal or no output (all should be 10.0.x)
```

### 6. Run Application

```bash
cd /Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee/deploy
bash run-docker-development.sh
```

**Verification Steps**:

1. **Container Status**:
   ```bash
   docker ps
   # Expected: moriicoffee.api, moriicoffee.database, moriicoffee.minio all running
   ```

2. **API Logs** (check for errors):
   ```bash
   docker logs moriicoffee.api
   # Expected: No errors, application started successfully
   ```

3. **Swagger UI**:
   - Open: http://localhost:8002/swagger
   - Expected: Swagger UI loads, all endpoints displayed

4. **Database Connection**:
   ```bash
   docker logs moriicoffee.api | grep -i "database"
   # Expected: "Database migration completed" or similar success message
   ```

5. **MinIO Connection**:
   ```bash
   docker logs moriicoffee.api | grep -i "minio"
   # Expected: No connection errors
   ```

---

## Manual Feature Testing Checklist

### Authentication & Authorization

- [ ] **User Registration (Email/Password)**:
  - POST `/api/auth/register` with email/password
  - Expected: 200 OK, user created, verification email sent

- [ ] **User Login (Email/Password)**:
  - POST `/api/auth/login` with email/password
  - Expected: 200 OK, JWT token returned

- [ ] **Google OAuth Registration**:
  - GET `/api/auth/google` (initiates OAuth flow)
  - Complete Google sign-in
  - Expected: Redirect to app, user registered, JWT token issued

- [ ] **Google OAuth Login**:
  - GET `/api/auth/google` (initiates OAuth flow)
  - Complete Google sign-in for existing user
  - Expected: Redirect to app, JWT token issued

- [ ] **JWT Token Validation**:
  - Call protected endpoint with valid JWT
  - Expected: 200 OK, endpoint responds
  - Call protected endpoint without JWT
  - Expected: 401 Unauthorized

- [ ] **Token Refresh** (if implemented):
  - POST `/api/auth/refresh` with refresh token
  - Expected: 200 OK, new access token issued

### Email Service (Brevo)

- [ ] **Welcome Email**:
  - Register new user
  - Check email for welcome message
  - Expected: Email received, HTML template renders correctly

- [ ] **Verification Email**:
  - Register new user
  - Check email for verification link
  - Click link
  - Expected: Account verified successfully

### Database Operations (EF Core)

- [ ] **User CRUD**:
  - Create user (POST `/api/users`)
  - Read user (GET `/api/users/{id}`)
  - Update user (PUT `/api/users/{id}`)
  - Delete user (DELETE `/api/users/{id}`)
  - Expected: All operations succeed, data persists

- [ ] **Database Migrations**:
  - Check logs for migration messages
  - Expected: "Database migration completed successfully"

- [ ] **Complex Queries**:
  - GET `/api/products?category=X&sort=price`
  - Expected: Filtered and sorted results

- [ ] **Nullable Handling**:
  - Query entities with nullable properties
  - Expected: No NullReferenceExceptions in logs

### File Storage (MinIO)

- [ ] **File Upload**:
  - POST `/api/files/upload` with multipart form data
  - Expected: 200 OK, file uploaded to MinIO

- [ ] **File Download**:
  - GET `/api/files/{id}` for uploaded file
  - Expected: 200 OK, file content returned

- [ ] **MinIO Console Verification**:
  - Login to MinIO console: http://localhost:9001
  - Check bucket for uploaded files
  - Expected: Files visible in bucket

### API Endpoints

- [ ] **Swagger Documentation**:
  - Open http://localhost:8002/swagger
  - Expected: All endpoints listed, schemas displayed

- [ ] **Product Endpoints**:
  - GET `/api/products` — Expected: 200 OK, products list
  - GET `/api/products/{id}` — Expected: 200 OK, product details
  - POST `/api/products` — Expected: 201 Created (if authenticated)

- [ ] **Category Endpoints**:
  - GET `/api/categories` — Expected: 200 OK, categories list

- [ ] **Banner Endpoints**:
  - GET `/api/banners` — Expected: 200 OK, banners list

- [ ] **Validation Errors**:
  - POST `/api/auth/register` with invalid email
  - Expected: 400 Bad Request, validation errors in response

### Application Startup & Infrastructure

- [ ] **Docker Build Success**:
  ```bash
  docker build -t test-build -f source/MoriiCoffee.Presentation/Dockerfile .
  ```
  - Expected: Build succeeds, no errors

- [ ] **Container Startup**:
  - All containers start without errors
  - Health checks pass (database)

- [ ] **Configuration Loading**:
  - Check logs for config messages
  - Expected: Configuration loaded from appsettings.json

- [ ] **Logging Initialization**:
  - Check logs for Serilog initialization
  - Expected: Structured logging output, no errors

- [ ] **No Deprecation Warnings**:
  ```bash
  docker logs moriicoffee.api 2>&1 | grep -i "deprecat\|obsolete\|warning"
  ```
  - Expected: Minimal or no deprecation warnings related to .NET 10 upgrade

---

## Performance Validation

### Baseline Metrics (Pre-Upgrade)

Before upgrading, capture baseline metrics:

```bash
# API response time
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:8002/api/products

# Database query time (check logs)
docker logs moriicoffee.api | grep "Query execution time"
```

Create `curl-format.txt`:
```
time_namelookup:  %{time_namelookup}\n
time_connect:  %{time_connect}\n
time_appconnect:  %{time_appconnect}\n
time_pretransfer:  %{time_pretransfer}\n
time_redirect:  %{time_redirect}\n
time_starttransfer:  %{time_starttransfer}\n
----------\n
time_total:  %{time_total}\n
```

### Post-Upgrade Validation

After upgrading to .NET 10, repeat the same measurements:

```bash
# API response time
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:8002/api/products
```

**Success Criteria** (from spec.md):
- Application startup time within 10% of baseline
- API response times within acceptable range (< 200ms p95)
- Database query times within 10% of baseline

**Compare**:
- .NET 8 time_total: `X ms`
- .NET 10 time_total: `Y ms`
- Difference: `|Y - X| / X * 100%` should be < 10%

---

## Troubleshooting

### Issue: "SDK version not found"

**Symptom**:
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0.
```

**Solution**:
1. Verify .NET 10 SDK installed: `dotnet --list-sdks`
2. Check `global.json` specifies correct version
3. Update `global.json` or delete it to use latest installed SDK

```bash
# Check installed SDKs
dotnet --list-sdks

# If 10.0 SDK missing, install it
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 10.0
```

---

### Issue: "Package restore failed"

**Symptom**:
```
error NU1102: Unable to find package Microsoft.AspNetCore.* with version (>= 10.0.5)
```

**Solution**: Clear NuGet cache and retry
```bash
dotnet nuget locals all --clear
dotnet restore
```

---

### Issue: "Docker build fails with SDK error"

**Symptom**:
```
#8 0.505 /bin/sh: 1: dotnet: not found
```

**Solution**: Ensure Dockerfile uses correct .NET 10 base image
```dockerfile
# Verify these lines in Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

**Rebuild**:
```bash
docker build --no-cache -f source/MoriiCoffee.Presentation/Dockerfile .
```

---

### Issue: "Runtime errors after upgrade"

**Symptom**: Application starts but throws exceptions at runtime

**Solution**: Check breaking changes in `research.md`

Common issues:
1. **Authentication fails**: Check if PAR needs to be disabled for Google OAuth
   ```csharp
   options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
   ```

2. **EF Core queries fail**: Check nullable semantics, verify SQL Server version supports new query functions

3. **JSON serialization errors**: Check DTO models for duplicate property names (case-insensitive)

**Debugging**:
```bash
# Check logs for detailed error messages
docker logs moriicoffee.api --tail 100

# Check for specific error patterns
docker logs moriicoffee.api 2>&1 | grep -i "exception\|error"
```

---

### Issue: "Obsolete API warnings during build"

**Symptom**:
```
warning CS0618: 'SomeMethod' is obsolete: 'Use NewMethod instead'
```

**Solution**: Since `TreatWarningsAsErrors` is enabled in Directory.Build.props, obsolete API warnings will cause build failures.

1. Review the warning message for recommended alternative
2. Update code to use new API
3. If temporary workaround needed, suppress specific warning in .csproj:
   ```xml
   <NoWarn>$(NoWarn);CS0618</NoWarn>
   ```

---

### Issue: "Identity authentication not working"

**Symptom**: Login fails with 500 Internal Server Error

**Likely Cause**: Obsolete `Microsoft.AspNetCore.Identity` 2.2.0 package not removed

**Solution**: Verify package removed from MoriiCoffee.Application.csproj
```bash
grep "Microsoft.AspNetCore.Identity" source/MoriiCoffee.Application/MoriiCoffee.Application.csproj
# Should NOT show version 2.2.0 (only Microsoft.AspNetCore.Identity.EntityFrameworkCore should exist)
```

**Fix**: Remove the line if still present, rebuild, restart

---

## Rollback Procedure

If critical issues arise that cannot be resolved:

### 1. Checkout Previous Branch

```bash
# Navigate to repository root
cd /Users/zephyr.nguyen/dev-space/projects/morii/morii-coffee

# Checkout previous stable branch
git checkout 005-google-oauth  # or main
```

### 2. Rebuild Docker Images

```bash
cd deploy
bash run-docker-development.sh
```

This will rebuild containers with .NET 8 from the previous branch.

### 3. Verify Rollback

```bash
# Check .NET version in container
docker exec moriicoffee.api dotnet --version
# Expected: 8.0.x

# Verify API works
curl http://localhost:8002/swagger
```

### 4. Report Issues

Document the failure for investigation:

1. Capture error logs: `docker logs moriicoffee.api > upgrade-failure.log`
2. Note specific error messages
3. Create GitHub issue with details
4. Attach logs and steps to reproduce

---

## Success Criteria Verification

After completing the upgrade, verify all success criteria from `spec.md`:

- [ ] **SC-001**: All projects compile successfully with .NET 10
  ```bash
  dotnet build
  # Expected: Build succeeded. 0 Error(s)
  ```

- [ ] **SC-002**: Docker containers build and start without errors
  ```bash
  cd deploy && bash run-docker-development.sh
  docker ps
  # Expected: All containers running
  ```

- [ ] **SC-003**: All features function identically (100% feature parity)
  - Execute full manual verification checklist above

- [ ] **SC-004**: Package restore succeeds with zero conflicts
  ```bash
  dotnet restore
  # Expected: Restore succeeded
  ```

- [ ] **SC-005**: Application startup time within 10% of baseline
  - Compare Docker logs timestamps for startup duration

- [ ] **SC-006**: Authentication flows work (email/password, Google OAuth)
  - Test all auth endpoints

- [ ] **SC-007**: Email sending works (Brevo)
  - Test registration, verify email received

- [ ] **SC-008**: Database operations work, query times within 10% of baseline
  - Test CRUD operations, compare query execution times in logs

- [ ] **SC-009**: Zero runtime exceptions in logs
  ```bash
  docker logs moriicoffee.api 2>&1 | grep -i "exception"
  # Expected: No exceptions related to upgrade
  ```

- [ ] **SC-010**: Third-party packages documented if flagged
  - Check `research.md` Section 2.3 for flagged packages

---

## Post-Upgrade Recommendations

### 1. Add Basic Integration Tests

The codebase currently has **zero automated tests**. Consider adding basic smoke tests:

```csharp
// Example: xUnit integration test
public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { email = "test@example.com", password = "Password123!" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

**Benefit**: Prevents regressions in future upgrades

---

### 2. Monitor Production After Deployment

If deploying to production:

1. **Enable Application Insights** (or similar monitoring)
2. **Set up alerts** for error rate increases
3. **Monitor performance** for first 24-48 hours
4. **Keep rollback plan ready**

---

### 3. Plan FluentValidation.AspNetCore Migration

The package is deprecated. Plan future migration to manual validator registration:

```csharp
// Current (deprecated)
services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());

// Future (manual registration)
services.AddValidatorsFromAssemblyContaining<Startup>();
builder.Services.AddFluentValidationAutoValidation();
```

---

## Summary

**Upgrade Verification**:
1. SDK version: .NET 10.0.102
2. Framework target: net10.0
3. Packages: All updated to .NET 10 versions
4. Obsolete packages: Removed
5. Build: Succeeds
6. Docker: Builds and runs
7. Features: All working
8. Performance: Within 10% baseline

**Next Steps After Successful Upgrade**:
1. Merge feature branch to main
2. Deploy to staging environment
3. Run full QA testing
4. Monitor production after deployment
5. Consider adding automated tests

**Documentation**: Keep this guide for future reference and team onboarding.
