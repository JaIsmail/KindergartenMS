
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


---

## Prod Environment (kms-api-prod-kg01) — Investigated, Paused (2026-06-18)
- Confirmed broken: container crashes with exit code 134 consistently ~28-30s into startup, every restart attempt
- Has been broken since at least 2026-06-06 (earliest available log), likely longer — never actually used/relied upon in this project
- Root causes ruled out: missing Jwt__Issuer/Jwt__Audience/FirebaseAdminSdk settings (added, no change), SQL password placeholder "YourStr0ngP@ssword!" (reset to real password, no change), container start timeout (extended via WEBSITES_CONTAINER_START_TIME_LIMIT, setting didn't appear to register, no change)
- IMPORTANT LESSON: kms-sqlserver-kg01 is SHARED between staging and prod databases. Resetting the server admin password (az sql server update --admin-password) broke staging's connection string until manually updated to match. Staging connection string updated and restored — confirmed working post-fix.
- New SQL admin password in use (shared server): KmsProd2026SecurePass — update both staging AND prod connection strings together if ever changed again
- No functional impact: staging has served as the de facto working environment for all development/testing throughout the project; prod has never been used
- Decision: paused further investigation given shared-infrastructure risk (already caused one near-miss to staging today); deprioritized in favor of Note 53 (dedicated demo subscription) as the path to a clean, isolated client-facing environment
- If revisited: use Kudu SSH console (kms-api-prod-kg01.scm.azurewebsites.net) for cleaner exception traces rather than docker.log/containerStream.log, which did not capture useful application-level stack traces

---

## CRITICAL BUG FOUND & FIXED: Hardcoded Staging API URL (2026-06-19)
- Root cause of today's prod investigation confusion: admin.html, app.html, and demo.html all had `const API = 'https://kms-api-staging-kg01.azurewebsites.net'` HARDCODED
- This meant EVERY deployment (prod, dev, any future environment) silently called staging's API for all data, regardless of which domain actually served the HTML page
- Curl tests correctly hit each environment's real API directly; browser tests were always silently redirected to staging via this hardcoded JS constant — explaining the day-long discrepancy between curl results (correct, empty prod) and browser results (showing staging's real data on prod's URL)
- Proven via controlled test: created uniquely-named child "STAGING_ONLY_TEST_CHILD_12345" on staging only, immediately appeared in prod's browser UI; curl to prod's actual API correctly showed empty results
- FIX: changed `const API = 'https://...'` to `const API = '';` in all three files — now uses relative URLs, correctly targeting whatever origin serves the page
- This bug existed since these files were created; never caught before because staging was the only environment ever actually tested via browser until today
- Verified post-fix: prod's UI now correctly shows only its own genuine data (1 SuperAdmin, 0 children, 0 tenants)
- IMPORTANT: this fix benefits ALL environments going forward, including any future demo/client subscription (Note 53) — no per-environment URL configuration needed, it now just works correctly based on deployment domain

---

## Prod = Client Testing Platform — Synced with Staging (2026-06-20)
- Used `az sql db copy` to clone KindergartenDB-staging -> KindergartenDB-prod-copy (same server, real-time copy of all live data: tenant مركز رواد العلم, 7 users, 1 child, subscriptions, payments)
- Deleted the empty/clean-schema KindergartenDB-prod created earlier in today's session
- Renamed KindergartenDB-prod-copy -> KindergartenDB-prod for consistency
- Updated kms-api-prod-kg01 connection string to point at the renamed database, restarted, verified working
- Verified via curl: /api/tenants on prod returns the real tenant data, matching staging exactly
- Prod (kms-api-prod-kg01.azurewebsites.net) is now ready for client testing with real, current data
- IMPORTANT: prod and staging are now two SEPARATE databases with a snapshot of the same data as of 2026-06-20 — they will diverge over time as each receives independent writes (this is intentional; prod is now a stable point-in-time copy for client demos, not a live mirror)
- If a fresh sync is needed later, repeat the `az sql db copy` step (will need to delete/rename again, or copy to a new name and switch the connection string)

---

## Note 49 — Complete Verification Summary (2026-06-20)
Two related but distinct permission-system bugs, both confirmed fixed:

1. **Duplicate PermissionGroups on re-seed** (SeedDefaultGroupsAsync): Fixed earlier with existingNames check. Verified via direct SQL query on staging AND prod: 0 duplicates, 6 groups each.

2. **Permission seed overriding/resetting existing data** (/api/permissions/seed endpoint): Previously fixed to upsert instead of delete-and-reinsert. Re-verified today on BOTH staging and prod:
   - POST /api/permissions/seed returns {"added":0} when all 50 permissions already exist (correct idempotent behavior)
   - Admin group's permission count remains at 50 after calling seed (no reset/override occurred)

Both fixes confirmed deployed and stable on staging and prod as of 2026-06-20. Note 49 fully closed, no remaining action items.
