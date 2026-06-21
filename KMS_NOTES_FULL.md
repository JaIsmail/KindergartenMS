
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

---

## Note 54: Account Dropdown Menu — Admin/App Header 🟡 PENDING (2026-06-20)
- Top-left avatar circle (currently just shows initial letter, e.g. "A") needs a dropdown on click
- Should show: full name, profile picture (placeholder/future feature), "تغيير كلمة المرور" (change password), "تسجيل الخروج" (sign out)
- Applies to both admin.html and app.html headers

---

## Note 51 — Progress Update (2026-06-20)
### Completed today:
1. Security fix: PaymentsController.GetBySubscription ownership verification (parents can only see their own children's payments) — verified with real second-parent test
2. Schema fix: dropped leftover Identity columns (TwoFactorEnabled, PhoneNumberConfirmed, SecurityStamp, NormalizedEmail, NormalizedUserName) from AspNetUsers on BOTH staging and prod — was causing 500 errors on Add User / registration
3. Logic fix: subscription.PaymentStatus now correctly reflects partial payments — "PartiallyPaid" when sum(payments) < price, "Paid" only when fully covered. Previously any single payment marked the whole subscription Paid regardless of amount.
4. UI: Payment History section built into app.html subscriptions page — click-to-expand per subscription showing: total price, total paid, remaining balance (color-coded red/green), and itemized payment list (amount, date, method, status badge, notes if present)
5. Verified end-to-end on prod with real multi-payment scenario: 500+300+50 against 1000 SAR subscription correctly shows PartiallyPaid with 150 SAR remaining

### Still pending (Note 51 full scope):
- Exam/academic results viewing
- Direct messaging with teachers/admin
- Subscription-creation notification (separate from Note 48's payment-confirmation notification)
- Admin-configurable notification message templates

---

## Note 55: Attendance Status Notification to Parent 🟡 PENDING (2026-06-20)
- When a child's attendance is marked, parent should be notified
- Admin needs flexibility to select status (Absent / Attended / other states)
- Admin should be able to write a customized text message for the notification (not just a fixed template)
- Likely ties into the broader "admin-configurable notification templates" need already noted under Note 51
- Needs design: trigger point (attendance check-in/out vs manual admin marking?), UI for status selection + message composer, notification delivery via existing INotificationService

---

## Note 51 — Subscription Creation Notification ✅ DONE (2026-06-20)
- SubscriptionService.CreateAsync now sends a notification to the parent when a new subscription is registered (parallel to Note 48's payment-confirmation notification)
- Uses existing INotificationService.SendToUserAsync, same pattern as PaymentService
- AR/EN messages: "تم تسجيل اشتراك جديد" / "New Subscription Registered" with subscription type, child name, price
- Verified on prod: subscription creation succeeds cleanly (200, no exceptions from notification code path)

### Note 51 — Remaining scope:
- Exam/academic results viewing (not started)
- Direct messaging with teachers/admin (not started)
- Admin-configurable notification message templates (not started — currently all notification text is hardcoded AR/EN in C#)

---
## Note 51 (continued) — Notification Trigger Registry & Admin UI (Phase 4) ✅ COMPLETE (2026-06-21)

### Completed in this session:
**7 actionable notification trigger keys fully wired and tested:**
1. `payment_confirmed` — pre-existing, confirmed working
2. `subscription_created` — pre-existing, confirmed working
3. `leave_request_submitted` — **bug fixed**: was calling `SendToAllParentsAsync` (broadcasting to ALL parents in tenant), now correctly targets `Leave.Approve` permission holders with Admin-role fallback for edge cases
4. `leave_request_reviewed` — **newly wired**: notifies employee when their leave request is approved/rejected, uses `{statusAr}`/`{statusEn}` placeholders for bilingual status text
5. `subscription_cancelled` — **newly wired**: notifies parent on subscription delete (captures child name + type before deletion, wrapped in try/catch)
6. `trip_started` — **migrated to template system + critical bug fixed**: was calling `SendToAllParentsAsync` (broadcasting to ALL tenant parents), now correctly filters to only parents of children actually on the specific trip via `TripChildren.Where(...).Select(...).Distinct()`
7. `trip_ended` — **same fixes as trip_started**: migrated templates, fixed broadcast-to-all bug
8. `child_registered` — **newly wired + bug fixed**: notifies parent when child is created, also fixed pre-existing `ParentName` bug in `ChildService.CreateAsync` that looked up caller's ID instead of assigned parent's ID when admin assigns child to a different parent
9. `attendance_marked` — remains correctly marked `Planned` (blocked on Note 55: child attendance feature doesn't exist yet)

**Backend infrastructure — single source of truth for all 9 keys:**
- New file: `src/Kindergarten.Core/Entities/NotificationKeyInfo.cs` defines `NotificationKeyInfo` class (key, category, AR/EN description, placeholders list, default AR/EN title+body, status enum `Wired`/`Planned`) and static `NotificationRegistry` class containing all 9 keys
- Refactored `NotificationService.DefaultTemplates.Get(key)` to delegate to `NotificationRegistry.GetDefaults(key)` instead of maintaining duplicate hardcoded switch statement — eliminates prior duplication risk
- New endpoint: `GET /api/notification-templates/registry` on `NotificationTemplatesController` returns static registry merged with per-tenant `hasCustomTemplate` flag, powering the dynamic admin UI dropdown

**Admin UI — fully dynamic notification templates dropdown:**
- Replaced hardcoded 3-option `<select>` in `admin.html` with dynamic population from `/api/notification-templates/registry`
- New `loadNotificationRegistry()` function: fetches registry, populates dropdown with all 9 keys, builds option labels showing AR description + comma-separated placeholders
- Status indicators in dropdown: ⏳ prefix for `Planned` keys, ✅ prefix for keys with custom per-tenant overrides
- `loadTemplateForKey()` refactored to pull default text from cached registry data instead of hardcoded `tplDefaults` JS object (eliminated)
- **Critical frontend bug discovered and fixed**: `loadDevices()` had an early `return` in its "no registered devices" branch that skipped `loadNotificationRegistry()` entirely — since most accounts in this project have zero registered FCM devices (known gap, Note 48), the entire templates dropdown was silently non-functional for the common case. Fixed by converting `return;` to `else {...}` structure, ensuring `loadNotificationRegistry()` runs unconditionally regardless of device count

**Real test data used & verified (tenant 6 — مركز رواد العلم):**
- AdminT1, teacher@rawad-center.com, JaberT1 (parent with 2 children + active FCM device)
- Created & tested: leave requests (submitted → reviewed), trips (created → started → ended), child registration, subscription cancellation
- All test data cleaned up after verification
- Verified all endpoints via live curl tests against prod; browser-verified end-to-end (Network tab + dropdown population + correct status icons)

**Key architectural decisions:**
- Notification scoping bugs (leave_request_submitted, trip notifications) traced to recurring pattern: code was using `SendToAllParentsAsync` (tenant-wide broadcast) when it should target specific permission holders or specific-trip parents
- Admin fallback: when no user holds a required permission, fall back to any user with `RoleType=="Admin"` as a safety net for fresh tenants
- Registry as single source of truth: eliminated duplicate hardcoded text in 3 places (C# `DefaultTemplates`, JS `tplDefaults`, hardcoded HTML dropdown options) — now all 9 keys defined once in `NotificationKeyInfo.cs`

### Related bugs fixed as side effects:
- `ChildService.CreateAsync` `ParentName` lookup bug (was using caller's ID instead of resolved parent ID)
- `LeaveRequestService` missing dedicated `GetUserIdsWithPermissionAsync` helper — added as reusable pattern for permission-based targeting

### Remaining scope (Note 51 / notifications):
- `attendance_marked` notification — unblocked once Note 55's child attendance feature is built
- Exam/academic results viewing (not touched)
- Direct messaging with teachers/admin (not touched)

