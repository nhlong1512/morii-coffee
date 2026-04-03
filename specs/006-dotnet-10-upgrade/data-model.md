# Data Model: N/A for Infrastructure Upgrade

**Feature**: 006-dotnet-10-upgrade
**Date**: 2026-04-02

---

## Overview

This feature is a **platform/infrastructure upgrade** from .NET 8 to .NET 10 and **does not introduce or modify domain entities**. The database schema remains unchanged, and all existing Entity Framework Core entities continue to function without modification.

---

## Impact on Existing Entities

**Status**: **NO CHANGES**

The .NET 10 upgrade is **transparent to the domain model**. All existing entities in the Morii Coffee system remain unchanged:

- **User** (AspNetUsers table)
- **UserLogin** (AspNetUserLogins table)
- **UserToken** (AspNetUserTokens table)
- **Product** (Products table)
- **Category** (Categories table)
- **Banner** (Banners table)
- Other domain entities

---

## Entity Framework Core Compatibility

**EF Core Version**: 8.0.10 → 10.0.5

**Backward Compatibility**: ✅ YES

- EF Core 10 maintains backward compatibility with existing database schemas
- Nullable reference types (`<Nullable>enable</Nullable>`) remain compatible
- Entity configurations remain valid
- Fluent API configurations remain valid
- Data annotations remain valid

**Query Translation Changes**: EF Core 10 improves query translation but does not break existing queries. See `research.md` Section 1.1 for details on:
- Nullable semantics enforcement
- GREATEST/LEAST function usage (requires SQL Server 2022+)
- Primitive collection support expansion

---

## Database Migrations

**Migration Required**: **NO NEW MIGRATIONS**

**Validation Required**: **YES** — Verify existing migrations still work with EF Core 10

**Steps**:
1. Run application with EF Core 10
2. Verify migrations table is current
3. Verify no migration warnings in logs
4. Test database operations (CRUD)

**No Schema Changes**: The upgrade does not alter the database schema. All tables, columns, indexes, and constraints remain unchanged.

---

## Seed Data

**Impact**: **NO CHANGES**

Existing seed data operations for Products, Categories, Banners, and Users remain unchanged. Verify seed data still populates correctly after upgrade.

---

## Testing Focus

Since the data model is unchanged, testing should focus on:

1. **Entity Materialization**: Verify entities hydrate from database correctly
2. **Query Execution**: Verify LINQ queries execute without errors
3. **Nullable Handling**: Verify nullable reference types don't cause runtime NullReferenceExceptions
4. **Migrations**: Verify existing migrations apply successfully
5. **Seed Data**: Verify seed operations complete successfully

---

## Summary

**Data Model Changes**: **NONE**

**Database Schema Changes**: **NONE**

**EF Core Changes**: **Version upgrade only** (8.0.10 → 10.0.5)

**Migration Required**: **NO** — Existing migrations remain valid

**Testing Focus**: Runtime behavior, query execution, nullable handling

**Risk Level**: **LOW** — EF Core 10 is backward compatible with existing schemas
