# Runtime Verification Checklist - .NET 10 Upgrade

**Status**: Ready for Docker runtime testing
**Created**: 2026-04-03
**Branch**: 006-dotnet-10-upgrade

---

## Prerequisites

1. Start Docker Desktop
2. Run: `cd deploy && bash run-docker-development.sh`
3. Wait for all containers to start (database, MinIO, API)
4. Verify API accessible at: http://localhost:8002/swagger

---

## Phase 5 Manual Verification Tasks (T072-T126)

### Authentication & Authorization (10 checks) - T072-T081

- [ ] **T072**: Email/password registration works (POST /api/auth/register)
- [ ] **T073**: Email/password login returns JWT token (POST /api/auth/login)
- [ ] **T074**: Google OAuth registration flow completes (GET /api/auth/google)
- [ ] **T075**: Google OAuth login works for existing user
- [ ] **T076**: JWT token validation on protected endpoints (with/without token)
- [ ] **T077**: Token refresh flow (if implemented)
- [ ] **T078**: Account linking between email and Google
- [ ] **T079**: User roles and permissions enforcement
- [ ] **T080**: Token expiration enforced correctly
- [ ] **T081**: No authentication errors in logs (`docker logs moriicoffee.api | grep -i auth`)

**Special Note for T074-T075**: If Google OAuth fails, check logs for PAR-related errors. If found, uncomment the PAR disable line in `source/MoriiCoffee.Infrastructure/Configurations/AuthenticationConfiguration.cs:75`:
```csharp
googleOptions.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
```

---

### Email Service (Brevo) (6 checks) - T082-T087

- [ ] **T082**: Welcome email sent on registration
- [ ] **T083**: Verification email sent and link works
- [ ] **T084**: Email templates render correctly (HTML embedded resources)
- [ ] **T085**: Brevo SDK communication succeeds (check logs)
- [ ] **T086**: Email delivery logging recorded
- [ ] **T087**: Failed email sends logged appropriately

---

### Database Operations (EF Core) (9 checks) - T088-T096

- [ ] **T088**: User CRUD operations work (create, read, update, delete)
- [ ] **T089**: AspNetUserLogins table records persist correctly
- [ ] **T090**: AspNetUserTokens table records persist correctly
- [ ] **T091**: Product CRUD operations work (if applicable)
- [ ] **T092**: Complex queries execute without errors
- [ ] **T093**: No NullReferenceExceptions from nullable reference types
- [ ] **T094**: Transaction handling for multi-step operations
- [ ] **T095**: Seed data populated correctly (Products, Categories, Banners, Users)
- [ ] **T096**: Change tracking works correctly

**Check migrations**: `docker logs moriicoffee.api | grep -i migration`
Expected: "Database migrations applied" or similar success message

---

### File Storage (MinIO) (6 checks) - T097-T102

- [ ] **T097**: MinIO container starts and initializes
- [ ] **T098**: File upload to MinIO works (POST /api/files/upload)
- [ ] **T099**: File download from MinIO works (GET /api/files/{id})
- [ ] **T100**: File deletion from MinIO works (DELETE /api/files/{id})
- [ ] **T101**: S3 fallback works (if configured)
- [ ] **T102**: No MinIO connection errors in logs

**Verify MinIO**: Open http://localhost:9001 and check bucket contains uploaded files

---

### API Endpoints (9 checks) - T103-T111

- [ ] **T103**: Authentication endpoints return correct status codes
- [ ] **T104**: Product endpoints return data correctly (GET /api/products)
- [ ] **T105**: Category endpoints return data correctly (GET /api/categories)
- [ ] **T106**: Banner endpoints return data correctly (GET /api/banners)
- [ ] **T107**: Request validation returns 400 Bad Request with error details
- [ ] **T108**: Request/response JSON serialization works correctly
- [ ] **T109**: Swagger UI displays all endpoints correctly
- [ ] **T110**: Swagger documentation generated correctly
- [ ] **T111**: CORS headers set correctly

---

### Application Startup & Infrastructure (10 checks) - T112-T121

- [ ] **T112**: Docker container builds without errors
- [ ] **T113**: Docker container starts without errors
- [ ] **T114**: Database connection established on startup
- [ ] **T115**: MinIO connection established on startup
- [ ] **T116**: All dependency injection services registered
- [ ] **T117**: Configuration loads from appsettings.json correctly
- [ ] **T118**: Environment variables override config as expected
- [ ] **T119**: Serilog logging initializes without errors
- [ ] **T120**: No startup console errors or warnings
- [ ] **T121**: Health check endpoint returns healthy (if available)

**Check startup logs**: `docker logs moriicoffee.api --tail 50`

---

### Framework & Language Features (5 checks) - T122-T126

- [ ] **T122**: Nullable reference handling doesn't cause exceptions
- [ ] **T123**: Implicit usings resolve correctly
- [ ] **T124**: LINQ operations work correctly
- [ ] **T125**: Async/await patterns work correctly
- [ ] **T126**: No obsolete API warnings in compilation output

---

### Performance Validation (4 checks) - T127-T130

- [ ] **T127**: API response times within acceptable range
  ```bash
  # Create curl-format.txt:
  cat > curl-format.txt << 'EOF'
  time_total:  %{time_total}
  EOF

  # Test API response time:
  curl -w "@curl-format.txt" -o /dev/null -s http://localhost:8002/api/products
  ```
  **Success Criteria**: Compare with baseline (should be within 10% of .NET 8 baseline)

- [ ] **T128**: Application startup time within 10% of baseline
  ```bash
  docker logs moriicoffee.api | grep "Application started"
  # Calculate time from container start to "Application started"
  ```

- [ ] **T129**: Database query performance within 10% of baseline
  ```bash
  docker logs moriicoffee.api | grep "Query execution time"
  # Compare average query times with baseline
  ```

- [ ] **T130**: Zero runtime exceptions related to upgrade
  ```bash
  docker logs moriicoffee.api 2>&1 | grep -i "exception" | grep -v "OperationCanceledException"
  # Expected: No exceptions related to .NET 10 upgrade
  ```

---

## Summary

**Total Manual Checks**: 55 verification points

**Estimated Time**: 2-3 hours for comprehensive testing

**Critical Areas**:
1. Google OAuth (potential PAR issue)
2. EF Core queries (nullable semantics, query translation)
3. Database migrations
4. Performance baseline comparison

**Success Criteria** (from spec.md):
- Zero functional regression (SC-003)
- Application startup within 10% of baseline (SC-005)
- Database queries within 10% of baseline (SC-008)
- Zero runtime exceptions (SC-009)

---

## Quick Smoke Test (5 minutes)

If time is limited, run this quick smoke test:

1. ✅ Containers start: `docker ps` shows all 3 containers running
2. ✅ Swagger loads: Open http://localhost:8002/swagger
3. ✅ Database connected: Check logs for "Database migrations applied"
4. ✅ Basic endpoint works: GET /api/products returns data
5. ✅ No errors in logs: `docker logs moriicoffee.api --tail 100` shows no exceptions

If all 5 pass → Upgrade likely successful, continue with full verification
If any fail → Investigate and fix before full verification
