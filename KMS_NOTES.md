# KMS Project Notes

---

## Note 1: Test Notification Cycle 🟡
- Create trip from Admin with driver + child
- Driver starts trip → notification to parent
- Driver picks up child → notification to parent
- Driver ends trip → notification to parent
- Verify notifications on browser and mobile

---

## Note 2: Fix Attendance Hours + Geo Restriction ✅ DONE
- ✅ Multiple check-in/check-out pairs per day
- ✅ Each period calculated separately
- ✅ Total hours = sum of all periods
- ✅ Geo restriction with dynamic radius from tenant settings
- ✅ Admin sets location via Settings page
- ✅ allowOutside toggle in attendance settings
- **Problem:** system calculates from first check-in to last check-out only
- **Required:**
  - Calculate each check-in/check-out pair separately
  - Sum all periods for actual total
  - Detailed log per check-in/check-out as sub-record
- **Geo Restriction:**
  - Attendance only works inside kindergarten radius
  - Admin sets location dynamically (lat, long, radius in meters)
  - Error message if employee is outside range

---

## Note 3: Dynamic Lists in Admin ✅ DONE
- Dynamic lists entity + migration + controller
- Admin can add/edit/delete class types, subscription types, trip statuses
- Dropdowns in Children and Subscriptions forms load dynamically
- Foldable grouped sidebar with SuperAdmin-only Tenants tab
- SuperAdmin impersonation now loads all tenant data correctly
- Admin can add/edit/delete:
  - Class types (KG1, KG2, etc.)
  - Subscription types
  - Trip statuses
  - Any other fixed lists in the system

---

## Note 4: Profile Picture + Password Update 🟡
- In profile page (PWA + MAUI):
  - Upload and change profile picture
  - Change password

---

## Note 5: Fix onchange on DateTime Field 🟡
- datetime-local field doesn't trigger onchange automatically on some devices
- Solution: use onblur instead or add a confirm button

---

## Note 6: Flexible Permissions System ✅ DONE
- **Pending Note 7:** Role dropdown in Add/Edit User will be replaced by Permission Groups
- **Role groups:**
  - Admin creates groups (driver, teacher, supervisor, nanny, etc.)
  - Each group has specific permissions
- **Assignment:**
  - At user creation or after
  - Group grants permissions automatically
  - Individual permissions can be added/removed
- **UI:** Users + Role Groups + Permissions under one section "Permissions & Roles"

---

## Note 7: Flexible Roles ✅ DONE (Partial)
- Old RoleGroups system removed
- PermissionGroups is now single source of truth
- SuperAdmin impersonation implemented
- SuperAdmin can manage any tenant
- Data isolation verified per tenant
- SystemRole added to PermissionGroup
- User role auto-assigned when group is assigned
- ✅ RoleType field added to PermissionGroup
- ✅ AuthService JWT role from PermissionGroup.RoleType
- ✅ All Authorize(Roles) replaced with RequirePermission or TenantId==0
- ✅ Default PermissionGroups seeded (Admin, Driver, Parent, Employee, Accountant, Supervisor)
- ✅ SuperAdmin identified by TenantId==0
- Remaining: Update Admin UI — replace Role dropdown with PermissionGroup selection
- Add new roles: accountant, supervisor, parent+employee
- User can hold more than one role
- Each role linked to a specific Tenant

---

## Note 8: Multi-Tenant SaaS ✅ FULLY COMPLETE
- Each kindergarten/school is an independent Tenant
- User linked to a specific kindergarten
- TenantId added to all tables
- SuperAdmin role added
- SuperAdmin dashboard
- Data isolation between Tenants
- **Completed:** Tenants table, TenantId in all tables+JWT, TenantMiddleware,
  TenantsController, Tenants page in Admin, ITenantService+TenantService,
  Global Query Filters in DbContext, TenantId in all Entities+Services,
  Migrations applied, tested on Azure, pushed to GitHub
- **Last migration:** AddTenantIdToTripChildAndLocation
- **Git commit:** b7a55f8

---

## Note 9: Payment System 🟠
- ✅ Phase 1 Complete:
  - Manual payment recording
  - Parent notification on payment confirmation
  - Payments report
  - Overdue subscriptions list
  - Dynamic alert days per tenant
- 🔜 Phase 2: Moyasar integration
- **Phase 1 (Manual):**
  - Admin records payment manually
  - Set amount, date, method
  - Change subscription status to Paid
  - Notify parent on confirmation
  - Payments report + overdue list
  - Alerts 7 and 3 days before due date
- **Phase 2 (Moyasar):**
  - Mada, STC Pay, Apple Pay
  - Tamara, Tabby (installments)
  - Payment link sent to parent
  - Auto-confirmation after payment

---

## Note 10: VS Code Setup 🟡
- Install: .NET 9 SDK, Azure CLI, Git
- Install Extensions: C# Dev Kit, Azure Tools, GitLens
- Clone repo
- Configure local secrets
- Run project locally
- Deploy to Azure from VS Code

---

## Note 11: Group Permissions Pages 🟡
- Merge Users + Role Groups + Permissions pages under one section in Admin

---

## Note 12: MAUI — Duplicate App Icon 🟢
- Two icons appearing on device after install

---

## Note 13: MAUI — Language Toggle 🟢
- Language toggle only available in login and profile pages
- Should be accessible from all pages

---

## Note 14: MAUI — English Translation Not Working 🟢
- Text stays in Arabic even when English is selected

---

## Note 15: MAUI — Logout Button Location 🟢
- Logout button should be in the profile page

---

## Note 16: MAUI — Driver Map Missing 🟢
- Driver map works in PWA but missing in MAUI

---

## Note 17: MAUI — Side Menu Missing 🟢
- Side menu not implemented in MAUI (works in PWA)

---

## Priority Order
| Priority | Notes |
|----------|-------|
| 🔴 Critical | 6, 7 |
| 🟠 Core | 9, 2, 3 |
| 🟡 Features | 1, 4, 5, 10, 11 |
| 🟢 MAUI | 12, 13, 14, 15, 16, 17 |

---

## Note 18: Add User — Missing Role Groups & Tenant Selection ✅ DONE
- When adding a new user, the form does not show:
  - Role groups to be selected
  - Tenant/entity list to assign the user to
- Required: dropdown for role group + dropdown for tenant in the Add User form

---

## Note 19: Tenant Update Permission ✅ DONE
- After creating a tenant, there is no way to update it
- Required:
  - Add "Update Tenant" as a recorded permission in the permissions list
  - This permission should be grantable to a user through their permission group
  - Only users with this permission can edit tenant details

---

## Note 20: Role Groups System ✅ DONE
- RoleGroup entity + RoleGroupPermission + UserRoleGroup tables
- RoleGroupsController with full CRUD
- Role Groups management page in Admin Dashboard
- JWT token includes role groups
- Users can be assigned to multiple role groups

---

## Note 21: Fix TenantId=0 Data ✅ DONE
- Added fix-tenant-data endpoint
- All existing data migrated to TenantId=1
- Leave requests, attendance, users all restored

---

## Note 20: Tenant City — Edit Modal Missing Dropdown 🟡
- Edit Tenant modal uses a text input for city instead of a dropdown
- Required: Replace city text input in Edit Tenant modal with the same dropdown used in Add Tenant modal

## Note 21: Tenant City — Stored in Arabic Only 🟡
- City is stored in Arabic in the DB
- Frontend should translate to English when language is EN
- City translation map added to tenant cards ✅
- Need to apply same translation to any other place city is displayed

---

## Note 22: Add User Form — Additional Fields 🟡
- Add the following fields to Add User and Edit User forms (PWA + Admin):
  - First Name + Last Name (split FullName into two fields)
  - Job Number
  - Tenant (already done in Add User)
  - Gender (Male/Female)
  - Status (Active/Inactive toggle)
- Update ApplicationUser entity with new fields
- Migration required

---

## Note 20: Role Groups System ✅ DONE
- RoleGroup entity + RoleGroupPermission + UserRoleGroup tables
- RoleGroupsController with full CRUD
- Role Groups management page in Admin Dashboard
- JWT token includes role groups
- Users can be assigned to multiple role groups

---

## Note 23: Standardize Permission Checks 🟡
- Some endpoints use only [Authorize] instead of [RequirePermission]
- Required: Add [RequirePermission] to ALL endpoints for consistency
- Affected controllers: TripsController, EmployeesController, DynamicListsController, PermissionGroupsController
- SuperAdmin impersonation token already has all permissions → no impact
- Regular users will only see what their group permissions allow

---

## Note 24: Settings Page Permissions 🟡
- Add "ManageSettings" permission to permissions list
- Settings page should only be visible to users with this permission
- Add to default Admin group permissions
- Hide settings nav item if user doesn't have ManageSettings permission

---

## Note 25: Settings Page Map Integration 🟡
- Map is not yet integrated with tenant location settings
- Required: Leaflet map should show pin at current tenant location
- Dragging pin or clicking map updates Lat/Lng fields automatically
- Circle on map shows the geo restriction radius
- Map needs to be initialized when settings page loads

---

## Note 26: Granular Permissions (CRUD Level) 🟠
- Current permissions are too broad (e.g. "ManageChildren" covers add+edit+delete)
- Required: Split permissions into CRUD actions per entity:
  - Children: ViewChildren, AddChild, EditChild, DeleteChild
  - Users: ViewUsers, AddUser, EditUser, DeleteUser
  - Trips: ViewTrips, AddTrip, EditTrip, DeleteTrip, TrackTrips
  - Subscriptions: ViewSubscriptions, AddSubscription, EditSubscription, DeleteSubscription
  - Payments: ViewFinancials, AddPayment, EditPayment, DeletePayment
  - Attendance: ViewAttendance, ManageAttendance, ViewOwnAttendance
  - Leave: ViewLeaveRequests, ManageLeaveRequests, SubmitLeaveRequest
  - Permissions: ManagePermissions, ManageRoleGroups
  - Settings: ViewSettings, ManageSettings
  - Notifications: SendNotifications
- Update all controllers to use granular permissions
- Update default PermissionGroup seeds with correct granular permissions
- This is a breaking change — requires migration and re-seeding

---

## Note 28: Remove Old RoleGroups System 🟡
- RoleGroups, RoleGroupPermissions, UserRoleGroups tables are duplicates
- PermissionGroups system is the correct one being used
- Remove: RoleGroupsController, RoleGroup entity, RoleGroupPermission entity, UserRoleGroup entity
- Remove: UserRoleGroups from JWT claims
- Migration to drop old tables
- This cleanup will simplify the codebase
