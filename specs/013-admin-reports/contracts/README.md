# Contracts — Admin Reports

This directory documents the backend interface contracts for the phase-1 admin reports feature.

## Included contracts

- [admin-reports-dashboard.md](./admin-reports-dashboard.md)
- [admin-reports-export.md](./admin-reports-export.md)

## Design notes

- Contracts follow the repo's existing response envelope conventions.
- Reports are admin-only.
- The first release keeps the interface focused on five sections:
  - summary cards
  - revenue trend
  - order status breakdown
  - top products
  - new-user trend
