# KMS Project Notes

---

## Note 1: Test Notification Cycle 🟡
- Create trip from Admin with driver + child
- Driver starts trip → notification to parent
- Driver picks up child → notification to parent
- Driver ends trip → notification to parent
- Verify notifications on browser and mobile

---

## Note 2: Fix Attendance Hours + Geo Restriction 🟠
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

## Note 3: Dynamic Lists in Admin 🟠
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
