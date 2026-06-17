
---

## Note 53: Dedicated Client Demo Environment 🟡 PENDING (started 2026-06-17)
- Goal: isolated, client-facing demo deployment unaffected by dev subscription billing/outages
- New Azure subscription created: "Rowad Al Elm" (ID: a4063f3b-5ab4-44c2-8175-1d05c034faf6)
- Completed so far:
  - Resource group: rg-kms-demo (uaenorth)
  - SQL Server: kms-sqlserver-demo (admin: kmsdemoadmin)
  - Database: KindergartenDB-demo (Basic tier)
  - Firewall rule: AllowAzureServices (0.0.0.0)
- BLOCKED: App Service Plan creation fails — subscription has 0 VM quota (new subscription default)
  - Tried regions: uaenorth, eastus — same error both times (subscription-wide, not region-specific)
  - Standard BS Family vCPUs quota shows 10 available, but App Service Plan VM allocation is a separate quota category showing 0
  - Action needed: submit quota increase request via Azure Portal > Quotas > My quotas > Compute provider > request increase for target region
  - May require manual Microsoft review (can take minutes to ~24h for new Pay-As-You-Go subscriptions)
- Remaining steps once quota approved:
  1. Create App Service Plan (B1, Linux) in rg-kms-demo
  2. Create Web App (kms-api-demo-client) with DOTNETCORE:9.0 runtime
  3. Copy app settings from staging (JWT keys, Firebase config) — swap connection string to new SQL DB
  4. Manual deploy from a stable git tag (not auto-CI/CD, to keep demo deploys deliberate)
  5. Run startup migrations/seed against fresh demo DB
  6. Seed clean demo data (not real dev/test data)
