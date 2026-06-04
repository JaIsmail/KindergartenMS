# KMS — Kindergarten Management System
# 📋 COMPLETE NOTES FILE — All Sessions Consolidated
# Last updated: 2026-06-01 (Notes 45–48 added)

---

## 🔗 Key Links
- Repo: github.com/JaIsmail/KindergartenMS
- Live URL: https://kms-api-staging-kg01.azurewebsites.net
- Admin: https://kms-api-staging-kg01.azurewebsites.net/admin.html
- PWA: https://kms-api-staging-kg01.azurewebsites.net/app.html
- APK: https://kmsstorage1779189591.blob.core.windows.net/apk/kms-latest.apk

## 👥 Test Accounts
| Email | Password | Role |
|-------|----------|------|
| superadmin@kms-platform.com | SuperAdmin@123456 | SuperAdmin |
| admin@kms-staging.com | Admin@123456 | Admin |
| parent.test@kms.com | Parent@123456 | Parent |
| Driver1@kms-staging.com | Driver@123456 | Driver |
| driver@kms-staging.com | Driver@123456 | Driver |
| employee@kms-staging.com | Employee@123456 | Employee |
| teacher@kms-staging.com | Teacher@123456 | Teacher (PermissionGroup) |

## 🚀 Standard Deploy Command
```bash
git add . && git commit -m "msg" && git push origin main && sleep 20 && gh run watch $(gh run list --limit 1 --json databaseId --jq '.[0].databaseId') --interval 5
```

## 💾 Backups
- Git Branch: backup/pre-multitenant-20260523
- Git Tag: v1.0-pre-multitenant
- DB Backup: kmsstorage1779189591/apk/backup-staging-20260523.bacpac

---

## ✅ COMPLETED NOTES

### Note 2: Fix Attendance Hours + Geo Restriction ✅ FULLY COMPLETE
- Attendance refactored from EmployeeId → UserId (string FK to AspNetUsers)
- Added AttendancePeriod entity — multiple check-in/checkout pairs per day
- Haversine geo restriction using tenant settings (lat, long, radius)
- PWA updated: GPS sent on check-in, open period detection, periods grid (دخول/خروج/المدة)

### Note 3: Dynamic Lists in Admin ✅ DONE
- Admin can add/edit/delete class types, subscription types, trip statuses

### Note 6: Flexible Permissions System ✅ DONE
- PermissionGroup entity + PermissionGroupPermission + UserPermissionGroup tables
- JWT embeds individual Permission claims (Option A — JWT-embedded permissions)
- PermissionHandler: checks JWT claims first → UserPermissions table → UserPermissionGroups (DB fallback)
- MapInboundClaims = false added to JWT config
- PermissionHandler registered as AddTransient
- User type/role determined entirely by PermissionGroup assignment — no hardcoded role names

### Note 7: Flexible Roles ✅ DONE (Partial — see remaining items below)
- SuperAdmin impersonation implemented
- SuperAdmin can manage any tenant
- Data isolation verified per tenant

### Note 8: Multi-Tenant SaaS ✅ FULLY COMPLETE
- Each kindergarten is an independent Tenant
- TenantId in all tables + JWT
- TenantMiddleware, Global Query Filters, ITenantService/TenantService
- Last migration: AddTenantIdToTripChildAndLocation
- Git commit: b7a55f8

### Note 18: Add User — Missing Role Groups & Tenant Selection ✅ DONE
- Dropdown for permission group + dropdown for tenant in Add User form

### Note 19: Tenant Update Permission ✅ DONE
- "Update Tenant" permission added and grantable via permission group

### Note 20: Role Groups System ✅ DONE
- RoleGroup entity + RoleGroupPermission + UserRoleGroup tables
- RoleGroupsController with full CRUD
- Role Groups management page in Admin Dashboard
- JWT token includes role groups

### Note 21: Fix TenantId=0 Data ✅ DONE
- fix-tenant-data endpoint added
- All existing data migrated to TenantId=1

### Note 27: Settings Page Bug ✅ DONE
- GET /api/tenants was not returning the Settings field (missing from Select projection in TenantsController)
- Fix: added Settings to the TenantsController projection
- Saves now persist correctly on reload

### Note 31: Leave Request System ✅ DONE
- LeaveRequest refactored from EmployeeId → UserId directly
- Submit leave request from PWA (teacher/employee)
- Admin approve/reject with notification
- Admin notified on new leave request submission
- Tested with teacher@kms-staging.com
- Git commit: dfa43b0

---

## 🔴 CRITICAL / BLOCKING

### Note 7 (Remaining): Flexible Roles 🔴 PARTIAL
- Unify Role dropdown with Permission Groups in Add/Edit User
- Add new roles: accountant, supervisor, parent+employee
- User can hold more than one role
- Each role linked to a specific Tenant
- Remove ClaimTypes.Role from JWT (no business logic should depend on it)
- Remove hardcoded `Role = "Admin"` from impersonation token in TenantsController

### Note 26: Granular Permissions (CRUD Level) 🟠
- Current permissions are page-level (e.g. ViewChildren)
- Required: CRUD-level (Children.View, Children.Add, Children.Edit, Children.Delete)
- Affects all controllers: Children, Trips, Subscriptions, Payments, Users, Employees

### Note 28: Remove Old RoleGroups System 🟡
- RoleGroups DB table exists from migration but has no entity class (dead code)
- Remove: RoleGroups table, migration references, JWT RoleGroups claim
- Keep: PermissionGroups (this IS the correct system)

---

## 🟠 CORE (High Priority)

### Note 9: Payment System 🟠
- **Phase 1 (Manual) — partially done:**
  - Admin records payment manually ✅
  - Set amount, date, method ✅
  - Change subscription status to Paid ✅
  - **BUG:** Admin UI uses field names (status, amount, dueDate) that don't match API response (paymentStatus, price, endDate) — NULL fields shown in modal
  - Notify parent on confirmation 🔴 PENDING
  - Payments report + overdue list ✅
  - Alerts 7 and 3 days before due date 🔴 PENDING
- **Phase 2 (Moyasar):**
  - Mada, STC Pay, Apple Pay
  - Tamara, Tabby (installments)
  - Payment link sent to parent
  - Auto-confirmation after payment
  - Endpoints needed: POST /api/payments/initiate, POST /api/payments/webhook, GET /api/invoices

### Note 23: Standardize Permission Checks 🟠
- ChildrenController: replace `role == "Admin"` checks with `[RequirePermission("ViewAllChildren")]` and `[RequirePermission("AssignChildToParent")]`
- SubscriptionsController: replace `role == "Admin"` with `[RequirePermission("ViewAllSubscriptions")]`
- TenantsController: remove hardcoded `ClaimTypes.Role = "Admin"` in impersonation token
- NotificationService: keep `RoleType` on ApplicationUser for DB-level queries only (not auth)
- Seed 3 new permissions: ViewAllChildren, AssignChildToParent, ViewAllSubscriptions → add to default Admin PermissionGroup

---

## 🟡 FEATURES (Normal Priority)

### Note 1: Test Notification Cycle 🟡
- Create trip from Admin with driver + child
- Driver starts trip → notification to parent
- Driver picks up child → notification to parent
- Driver ends trip → notification to parent
- Verify notifications on browser and mobile

### Note 4: Profile Picture + Password Update 🟡
- In profile page (PWA + MAUI):
  - Upload and change profile picture
  - Change password

### Note 5: Fix onchange on DateTime Field 🟡
- datetime-local field doesn't trigger onchange automatically on some devices
- Solution: use onblur instead or add a confirm button

### Note 10: VS Code Setup 🟡
- Install: .NET 9 SDK, Azure CLI, Git
- Install Extensions: C# Dev Kit, Azure Tools, GitLens
- Clone repo, configure local secrets, run locally, deploy from VS Code

### Note 11: Group Permissions Pages 🟡
- Merge Users + Role Groups + Permissions under one section "Permissions & Roles" in Admin

### Note 20b: Tenant City — Edit Modal Missing Dropdown 🟡
- Edit Tenant modal uses text input for city — replace with same dropdown as Add Tenant modal

### Note 21b: Tenant City — Stored in Arabic Only 🟡
- City stored in Arabic in DB
- Frontend should translate to English when language is EN
- City translation map added to tenant cards ✅
- Apply same translation to all other places city is displayed

### Note 22: Add User Form — Additional Fields 🟡
- Add User form missing fields: department, job title, national ID, profile picture upload
- Required for HR/payroll phase

### Note 24: Settings Page Permissions 🟡
- Settings page should be protected by a specific permission (e.g. ManageTenantSettings)
- Currently accessible to all admins without granular control

### Note 25: Settings Page Map Integration 🟡
- Settings page has lat/long fields for geo restriction
- Should show a Leaflet map so admin can click to set location visually
- Currently admin must enter coordinates manually

### Note 29: Delete Functions & Audit Log 🟡
- Soft delete on all major entities (IsDeleted flag + DeletedAt + DeletedBy)
- Hard delete option for SuperAdmin only
- Audit log table: entity, action (create/update/delete), userId, tenantId, timestamp, old/new values

### Note 30: Timezone — KSA (UTC+3) 🟡
- All DateTime fields in DB stored as UTC
- Display layer should convert to KSA (UTC+3) in admin UI and PWA
- Trip times, attendance check-in/out, leave request dates all affected

### Note 32: Leave Request Details Modal Fix 🟡
- Leave request details modal in admin does not show all fields correctly
- Missing: user full name, department, leave type label (translated), duration in days

---

## 🟢 MAUI (Low Priority)

### Note 12: MAUI — Duplicate App Icon 🟢
- Two icons appearing on device after install

### Note 13: MAUI — Language Toggle 🟢
- Language toggle only available in login and profile pages
- Should be accessible from all pages

### Note 14: MAUI — English Translation Not Working 🟢
- Text stays in Arabic even when English is selected

### Note 15: MAUI — Logout Button Location 🟢
- Logout button should be in the profile page

### Note 16: MAUI — Driver Map Missing 🟢
- Driver map works in PWA but missing in MAUI

### Note 17: MAUI — Side Menu Missing 🟢
- Side menu not implemented in MAUI (works in PWA)

---

## 🆕 NEW NOTES (Added from Performance Benchmarks session — 2026-06-01)

### Note 33: Add Composite DB Indexes (TenantId) 🔴 CRITICAL
- Missing indexes causing +35% query overhead post-multi-tenancy
- Required indexes:
  - Children(TenantId, IsActive)
  - Trips(TenantId, Status)
  - TripChildren(TripId, TenantId)
  - Attendance(TenantId, Date)
  - Subscriptions(TenantId, ExpiryDate)
  - LeaveRequests(TenantId, Status)
  - UserDevices(UserId, TenantId)
- Add as a new EF Core migration
- Expected result: query overhead drops from +35% → +4%

### Note 34: Enable Always-On on Azure App Service 🔴 CRITICAL
- Cold start currently 4.2s — target ≤ 1s
- Enable Always-On: Azure Portal → App Service → Configuration → General Settings → Always On = ON
- Requires B2 or higher plan (already on B2 ✅)

### Note 35: Add .AsNoTracking() to All Read-Only GET Endpoints 🟡
- All GET endpoints that don't write back to DB should use .AsNoTracking()
- Affects: ChildrenController, TripsController, SubscriptionsController, EmployeesController
- Expected: ~20% reduction in EF Core memory and CPU on read-heavy paths

### Note 36: Fix N+1 on Trips → TripChildren 🟡
- TripsController likely lazy-loads TripChildren per trip (N+1)
- Fix: use .Include(t => t.TripChildren).ThenInclude(tc => tc.Child)
- Also check TripsController → TripLocations for same pattern

### Note 37: Enable Response Compression Middleware 🟡
- Add app.UseResponseCompression() + services.AddResponseCompression() in Program.cs
- JSON payloads shrink 60–80%
- High impact on mobile (MAUI driver app GPS payloads)
- 3-line change in Program.cs

### Note 38: Cache Key Vault Secrets Locally 🟡
- Key Vault lookup currently hits Azure every time (~80ms added to cold paths)
- Use IMemoryCache with 1-hour TTL or built-in AzureKeyVault provider with refresh interval
- Removes ~80ms from token validation on cold startup

### Note 39: Paginate /api/reports/attendance Endpoint 🟡
- Endpoint does full table scan per tenant — 900ms current, target 150ms
- Add ?page=&pageSize= parameters
- Add LIMIT clause to EF query
- Add index on Attendance(TenantId, Date)

### Note 40: Add Application Insights / Azure Monitor 🟡
- No real performance monitoring currently in place — all benchmarks are estimated
- Add Application Insights SDK to measure real P50/P95 latencies, dependency traces, exception rates
- Required before production go-live
- NuGet: Microsoft.ApplicationInsights.AspNetCore

### Note 41: Add EF Core Query Logging in Dev/Staging 🟡
- Enable in Program.cs for dev environment:
  ```csharp
  options.EnableSensitiveDataLogging()
         .LogTo(Console.WriteLine, LogLevel.Information);
  ```
- Will surface slow queries, N+1 patterns, and missing indexes immediately

### Note 42: Upgrade Azure App Service Plan to S2 Standard 🔵 (Pre-Production)
- Current plan: B2 Basic — no autoscale, limited connections
- Upgrade to S2 Standard before production launch
- Enables autoscale rules and reduces SQL DTU pressure

### Note 43: Load Test Before Phase 3 Go-Live 🔵 (Pre-Production)
- Run k6 or NBomber load test against staging now to establish baseline
- Scenarios:
  - Smoke: 10 users, 5 min — P95 ≤ 300ms, 0 errors
  - Normal: 50 users, 15 min — P95 ≤ 400ms, err < 1%
  - Peak (multi-tenant): 200 users, 10 min — P95 ≤ 600ms
  - Soak: 30 users, 2hr — no memory leak
  - SignalR/GPS stress: 50 drivers, 30 min — fan-out ≤ 100ms
- Re-run after each Phase 3 feature to catch regressions

### Note 44: MAUI Driver GPS Batching 🟢
- MAUI driver app likely sends location updates per GPS point
- Batch payloads every 3–5 seconds to reduce SignalR/API throughput load
- Reduces server-side SignalR fan-out pressure during active trips

### Note 45: Payment Status as Dynamic List 🟠
- Payment status values (Pending, Paid, Overdue, Cancelled, Refunded) are currently hardcoded strings in the Subscription and Payment entities and in the admin UI dropdowns
- **Required:**
  - Add `PaymentStatuses` to the dynamic lists system (Note 3 / same pattern as class types, subscription types, trip statuses)
  - Admin can add/edit/delete payment status values from the Dynamic Lists page
  - Values stored in a `LookupItems` table (or equivalent) with Type = "PaymentStatus"
  - All dropdowns in admin UI (subscriptions page, payments modal) load from API instead of hardcoded options
  - Existing hardcoded statuses (Pending, Paid, Overdue) seeded as defaults on first run
  - API: reuse existing dynamic lists endpoint or add `GET /api/lists/payment-statuses`
- **Affected files:** admin.html (subscriptions + payments dropdowns), SubscriptionsController, PaymentsController, Subscription entity (PaymentStatus field stays as string — value comes from dynamic list)

### Note 46: Apply CRUD Permissions to Subscriptions & Payments 🟠
- Currently Subscriptions and Payments controllers use a single broad `[RequirePermission]` or role check
- **Required:** Break into granular CRUD-level permissions, consistent with Note 26 approach
- **Subscriptions permissions to seed:**
  - `Subscriptions.View` — list and view subscriptions
  - `Subscriptions.Add` — create new subscription for a child
  - `Subscriptions.Edit` — update subscription details, change dates/type
  - `Subscriptions.Delete` — cancel or remove a subscription
- **Payments permissions to seed:**
  - `Payments.View` — view payment records and history
  - `Payments.Add` — record a manual payment (Phase 1) or initiate Moyasar payment (Phase 2)
  - `Payments.Edit` — correct a payment record
  - `Payments.Delete` — void/remove a payment
- **Implementation steps:**
  1. Seed the 8 new permissions in `PermissionsController` seed endpoint
  2. Add them to the default Admin `PermissionGroup`
  3. Apply `[RequirePermission("Subscriptions.View")]` etc. to each action in `SubscriptionsController`
  4. Apply `[RequirePermission("Payments.View")]` etc. to each action in `PaymentsController`
  5. Update admin UI — hide/show subscription and payment action buttons based on user's permissions (same pattern as other pages)
- **Dependency:** Note 26 (Granular Permissions) should be completed first as it establishes the `Entity.Action` naming convention

### Note 47: Subscription Period Picker — Dynamic Calendar with Time Grabbers 🟠
- Currently the Add Subscription form has manual StartDate / EndDate text inputs with no visual period selection
- **Required:** Replace with a calendar-based period picker where admin selects a named period (e.g. "First Term 2026", "Full Year 2025–2026") or defines a custom date range using drag handles (time grabbers)

#### Dynamic Period Lists
- Admin manages subscription periods from the Dynamic Lists page (same pattern as Note 3 / Note 45)
- Each period has: `Name` (AR + EN), `StartDate`, `EndDate`, `IsActive`
- Stored in `LookupItems` table with Type = "SubscriptionPeriod"
- API: `GET /api/lists/subscription-periods` → returns active periods
- When admin selects a period from the dropdown, StartDate and EndDate auto-fill in the form
- Admin can also choose "Custom" to use the manual calendar grabbers instead

#### Calendar with Time Grabbers (UI)
- Visual inline calendar rendered in the Add/Edit Subscription modal
- Two draggable handles on the calendar: one for start date, one for end date
- Dragging a handle highlights the selected range in real time
- Below the calendar: two time inputs (HH:MM) to set exact start/end time if needed (for half-term or partial periods)
- Selected range shown as a summary: "15 March 2026 → 30 June 2026 (107 days)"
- On mobile (PWA): tap-to-select start then tap-to-select end (no drag)

#### Affected Files
- `admin.html` — Add Subscription modal: replace date inputs with period dropdown + calendar widget
- `SubscriptionsController` — no backend change needed; StartDate/EndDate already exist
- `LookupItems` / Dynamic Lists API — add SubscriptionPeriod type
- Admin Dynamic Lists page — add "Subscription Periods" section with CRUD

#### Dependencies
- Note 3 (Dynamic Lists) ✅ — base pattern already done
- Note 45 (Payment Status as Dynamic List) — same LookupItems table, same pattern

### Note 48: Parent Notification on Subscription Payment Completion 🟡
- When admin records a payment against a subscription (Phase 1 manual) and the subscription status changes to Paid, the parent linked to that subscription must receive a notification
- Same trigger should apply when Moyasar webhook confirms payment (Phase 2)

#### Notification Content
- **FCM push** to parent's registered device (via UserDevices table)
- **In-app notification** visible in the PWA notifications panel
- Message (AR): "تم تأكيد دفع اشتراك طفلك [ChildName] بنجاح ✅ — الفترة: [StartDate] إلى [EndDate]"
- Message (EN): "Your child [ChildName]'s subscription payment has been confirmed ✅ — Period: [StartDate] to [EndDate]"

#### Implementation
- Trigger point: `PaymentsController` → after payment is recorded and subscription `PaymentStatus` is set to "Paid"
- Call `INotificationService.SendToParent(parentId, title, body)`
- Phase 2: same call inside the Moyasar webhook handler after status verified
- No new endpoint needed — hook into existing payment save flow

#### Dependencies
- Note 9 Phase 1 (Manual payments) — trigger already exists, just needs notification call added
- Note 9 Phase 2 (Moyasar) — webhook handler will need the same call
- Note 1 (Notification cycle test) — should be verified together

---

## 📊 PRIORITY SUMMARY

| Priority | Notes |
|----------|-------|
| 🔴 Critical | 7 (remaining), 33, 34 |
| 🟠 Core | 9, 23, 26, 45, 46, 47 |
| 🟡 Features | 1, 4, 5, 10, 11, 20b, 21b, 22, 24, 25, 28, 29, 30, 32, 35, 36, 37, 38, 39, 40, 41, 48 |
| 🔵 Pre-Production | 42, 43 |
| 🟢 MAUI | 12, 13, 14, 15, 16, 17, 44 |

## ✅ DONE SUMMARY
Notes completed: 2, 3, 6, 8, 18, 19, 20, 21, 27, 31

## 📈 PROGRESS
- Total notes: 48
- Completed: 10 (21%)
- In progress / partial: 2 (Notes 7, 9)
- Pending: 36 (75%)
---

## Note 49: Remove `Value` Field from DynamicLists 🔵 (Pre-Production)
- **Phase 1 ✅ DONE:** Value field hidden from UI, auto-set from NameEn, dropdowns use nameEn
- **Phase 2 (Before Production):**
  - Data cleanup migration: set Value = NameEn for all existing rows
  - Update backend hardcoded comparisons (e.g. trip.Status == "InProgress")
  - EF Core migration to remove Value column from DynamicLists table
  - Update API response — remove value from JSON
  - Update MAUI — switch from item.value to item.nameEn

---

## Note 52: Permission Groups Seed — Critical Fix ✅ DONE
- **Problem:** Seed endpoint was destructive — deleted all existing groups then recreated from scratch
- **Impact:** All user-group assignments lost when seed was called
- **Fix:** Seed endpoint now additive only — never deletes existing groups
- **Fix:** Seed endpoint accepts `?tenantId=X` parameter for SuperAdmin use
- **Fix:** SuperAdmin calling seed without tenantId now returns 400 (prevented accidental TenantId=0 seeding)
- **Lesson:** Never call `/api/permission-groups/seed` on a live tenant — only for new tenants
- **Recovery procedure:** See recovery commands in this session

## Note 53: Permission Groups Cleanup 🟡
- Multiple duplicate groups exist in DB (from repeated seed calls):
  - Groups 19,27,33,45 all named "Admin" with different TenantIds
  - Groups 20,28,34,46 all named "Driver" etc.
- Need to clean up duplicates — keep only latest set per tenant
- Before cleanup: ensure all users are assigned to the correct latest group
- This is non-urgent but should be done before production launch

---

## Note 50 Phase 2: Subscription Registration ✅ DONE
- Child picker (search by name/parent) replaces manual ChildId input
- Auto-set ParentId from selected child's parent
- Warning if child already has active subscription
- Period dropdown from Dynamic Lists (Periods category)
- Subscriptions table shows: Child, Parent, Type, Period, Price, Duration, Status
- Delete subscription with styled confirmation modal
- Subscription count badge in sidebar
- Period display uses name lookup (AR/EN) instead of raw stored value
- Fixed: ChildService.CreateAsync uses dto.ParentId not caller's userId
- Fixed: AssignToUser uses target user's TenantId
