# Contracts: GHN Sandbox Integration

This folder documents the backend-facing contracts Morii will expose for the GHN sandbox integration.

## Files

- `shipping-master-data.md`: province, district, and ward reads used by checkout
- `shipping-quotes.md`: create/requote quote contracts and shared quote DTOs
- `order-shipment-lifecycle.md`: order payload changes, shipment reads, admin actions, and webhook contract

## Contract Principles

- Frontend communicates only with Morii backend.
- Contracts remain Morii-native, even when backed by GHN.
- Public read/write contracts are separated from admin-only shipment actions.
- Webhook ingestion is an operational contract, not a frontend contract.
- Development may use a local-only TLS bypass for GHN sandbox certificate issues, but the HTTP contracts remain identical across environments.
