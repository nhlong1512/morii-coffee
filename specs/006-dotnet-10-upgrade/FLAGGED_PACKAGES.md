# Flagged Packages - Future Attention Required

**Feature**: 006-dotnet-10-upgrade
**Date**: 2026-04-03
**Status**: Documented for future action

---

## Deprecated Packages (Still Works)

### 1. FluentValidation.AspNetCore 11.3.1

**Status**: Deprecated by maintainers but still functional

**Issue**: Package marked as "legacy" and "no longer maintained" by FluentValidation team

**Current Impact**: None - works correctly with .NET 10 and FluentValidation 12.1.1

**Recommended Action**: Plan future migration to manual validator registration

**Migration Path**:
```csharp
// Current (deprecated) - in DependencyInjection.cs:
services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());

// Future (manual registration):
services.AddValidatorsFromAssemblyContaining<Startup>();
builder.Services.AddFluentValidationAutoValidation();
```

**Timeline**: Non-urgent - can be addressed in next major refactoring

**References**:
- FluentValidation docs: https://docs.fluentvalidation.net/en/latest/aspnet.html
- GitHub issue: https://github.com/FluentValidation/FluentValidation/issues/1965

---

## Packages with Version Constraints

### 2. MicroElements.Swashbuckle.FluentValidation 6.0.0

**Status**: Version constraint mismatch (suppressed via NU1608)

**Issue**: Package officially supports FluentValidation >= 10.0.0 && < 12.0.0, but we're using FluentValidation 12.1.1

**Current Impact**: Works in practice despite version constraint warning

**Suppression**: Added `NU1608` to `Directory.Build.props` NoWarn list

**Recommended Action**: Monitor for MicroElements.Swashbuckle.FluentValidation updates that support FluentValidation 12.x

**Alternative**: If issues arise, consider:
- Downgrading FluentValidation to 11.x (not recommended)
- Removing MicroElements integration and manually configuring Swagger validation examples
- Waiting for MicroElements package update

**Timeline**: Monitor quarterly for package updates

---

## Packages Kept at Older Versions

### 3. Swashbuckle.AspNetCore 6.7.2

**Status**: Intentionally kept at 6.7.2 instead of upgrading to 10.x

**Issue**: Swashbuckle 10.x has extensive breaking changes in Microsoft.OpenApi 2.x
- `Microsoft.OpenApi.Models` namespace removed
- `OpenApiReference` type changes
- Requires significant code refactoring in SwaggerConfiguration.cs

**Current Impact**: None - Swashbuckle 6.7.2 works perfectly with .NET 10

**Recommended Action**:
1. Monitor Swashbuckle 10.x/11.x for API stability
2. Plan dedicated upgrade task when ready
3. Estimated effort: 2-4 hours for code refactoring and testing

**Migration Complexity**: Medium
- Requires updating SwaggerConfiguration.cs OpenAPI type references
- Need to update from `Microsoft.OpenApi.Models` to `Microsoft.OpenApi` namespace
- May need API reference updates for security scheme configuration

**Timeline**: Can upgrade anytime - not urgent, purely for staying current

**References**:
- Swashbuckle 10.x changelog: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/releases
- Microsoft.OpenApi 2.x migration: https://github.com/microsoft/OpenAPI.NET/releases

---

## Suppressed Warnings Summary

Currently suppressing in `Directory.Build.props`:
- **NU1903**: AutoMapper vulnerability (unpatched across all versions)
- **NU1902**: Package vulnerabilities (will be resolved by future package updates)
- **NU1901**: Low severity vulnerabilities (will be resolved by future package updates)
- **NU1608**: MicroElements.Swashbuckle.FluentValidation version constraint (works despite warning)

**Action**: Review suppressions quarterly and remove as packages are updated

---

## Monitoring Schedule

**Quarterly Review** (every 3 months):
1. Check for FluentValidation.AspNetCore replacement updates
2. Check for MicroElements.Swashbuckle.FluentValidation 7.x or 8.x releases supporting FluentValidation 12.x
3. Check for Swashbuckle 11.x or 12.x with stable Microsoft.OpenApi integration
4. Review AutoMapper releases for NU1903 vulnerability patches
5. Update this document with findings

**Next Review**: July 2026

---

## No Action Required

The following packages were considered but require no special attention:

- **Minio 7.0.0**: Successfully upgraded from 6.0.3, .NET 10 support confirmed
- **AWSSDK.S3 4.0.20.2**: Successfully upgraded from 3.7.x, no issues
- **MediatR 14.1.0**: Successfully upgraded from 12.4.0, no breaking changes
- **AutoMapper 16.1.1**: Successfully upgraded from 14.0.0, no breaking changes
- **Serilog 4.3.1**: Successfully upgraded from 4.0.1, no issues

---

## Summary

**Total Flagged Items**: 3 packages
**Critical**: 0
**Monitoring**: 3
**Timeline**: Non-urgent, plan for next major version upgrade or dedicated maintenance sprint

All flagged packages are fully functional with .NET 10 and pose no immediate risk.
