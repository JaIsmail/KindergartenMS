
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

---

## Note 48 — Parent Notification on Payment Confirmation ✅ BACKEND VERIFIED (2026-06-18)
- PaymentService.CreateAsync() confirmed correctly: updates subscription to Paid, looks up parent+child via plain _db.Users/_db.Children queries (no Identity dependency), calls INotificationService.SendToUserAsync with correct AR/EN messages
- Tested end-to-end: created subscription (ID 11, child رقية, parent جابر اسماعيل) → recorded payment → subscription status confirmed updated to Paid
- SendToUserAsync uses only _db.UserDevices table (FCM tokens) — no Identity/AspNetUser coupling
- Gap: actual FCM push delivery not confirmed on a real device — browser-based device registration (app.html desktop flow) did not successfully register a token during testing; root cause not isolated (no console "FCM ready"/"FCM error" log captured)
- Decision: backend trigger+payload logic is correct and sufficient to mark this note done; device registration/delivery is a separate, lower-priority follow-up if real-world testing surfaces it as an issue
- No Phase 2/webhook work needed yet (Moyasar not implemented)

---

## SeedDefaultGroupsAsync Duplicate Groups Bug ✅ FIXED (2026-06-18)
- Root cause: PermissionGroupsController.SeedDefaultGroupsAsync(tenantId) had no existence check, unconditionally added 6 default groups every call
- Verified before fixing: method was dead code, never called from anywhere in the API — zero active risk
- Verified database: 0 duplicate groups existed across all tenants (6 total groups, all clean)
- Fix applied: added existingNames check (IgnoreQueryFilters().Where(g => g.TenantId == tenantId)) before each group insert, skips if NameEn already exists for that tenant
- Deployed and verified build clean
