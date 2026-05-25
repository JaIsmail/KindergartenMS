# KMS — Kindergarten Management System
## Complete Project Context

### 🔗 Key Links
- Repo: github.com/JaIsmail/KindergartenMS
- Live URL: https://kms-api-staging-kg01.azurewebsites.net
- Admin: https://kms-api-staging-kg01.azurewebsites.net/admin.html
- PWA: https://kms-api-staging-kg01.azurewebsites.net/app.html
- APK: https://kmsstorage1779189591.blob.core.windows.net/apk/kms-latest.apk

### 🏗️ Tech Stack
- Backend: ASP.NET Core 9
- Mobile: .NET MAUI Android
- Database: SQL Server (kms-sqlserver-kg01 / KindergartenDB-staging)
- Cloud: Azure App Service (kms-api-staging-kg01, rg-kms-prod)
- Storage: kmsstorage1779189591 (rg-kms-staging)
- Notifications: Firebase FCM (kms-app-6af5c)
- CI/CD: GitHub Actions
- Key Vault: kms-keyvault-kg01
- Real-time: SignalR
- Maps: Leaflet.js

### 👥 Test Accounts
| Email | Password | Role |
|-------|----------|------|
| superadmin@kms-platform.com | SuperAdmin@123456 | SuperAdmin |
| admin@kms-staging.com | Admin@123456 | Admin |
| parent.test@kms.com | Parent@123456 | Parent |
| Driver1@kms-staging.com | Driver@123456 | Driver |
| driver@kms-staging.com | Driver@123456 | Driver |
| employee@kms-staging.com | Employee@123456 | Employee |


### 📁 Solution Structure
```
src/Kindergarten.Api/Authorization/PermissionHandler.cs
src/Kindergarten.Api/Authorization/PermissionRequirement.cs
src/Kindergarten.Api/Authorization/RequirePermissionAttribute.cs
src/Kindergarten.Api/Controllers/AuthController.cs
src/Kindergarten.Api/Controllers/ChildrenController.cs
src/Kindergarten.Api/Controllers/DevicesController.cs
src/Kindergarten.Api/Controllers/EmployeesController.cs
src/Kindergarten.Api/Controllers/LeaveRequestsController.cs
src/Kindergarten.Api/Controllers/PaymentsController.cs
src/Kindergarten.Api/Controllers/PermissionGroupsController.cs
src/Kindergarten.Api/Controllers/PermissionsController.cs
src/Kindergarten.Api/Controllers/RoleGroupsController.cs
src/Kindergarten.Api/Controllers/SubscriptionsController.cs
src/Kindergarten.Api/Controllers/TenantsController.cs
src/Kindergarten.Api/Controllers/TripsController.cs
src/Kindergarten.Api/Controllers/UsersController.cs
src/Kindergarten.Api/Hubs/TripHub.cs
src/Kindergarten.Api/Middleware/TenantMiddleware.cs
src/Kindergarten.Api/Program.cs
src/Kindergarten.Core/DTOs/AuthResponseDto.cs
src/Kindergarten.Core/DTOs/ChildDto.cs
src/Kindergarten.Core/DTOs/EmployeeDto.cs
src/Kindergarten.Core/DTOs/LeaveRequestDto.cs
src/Kindergarten.Core/DTOs/LoginDto.cs
src/Kindergarten.Core/DTOs/NotificationDto.cs
src/Kindergarten.Core/DTOs/PaymentDto.cs
src/Kindergarten.Core/DTOs/PermissionDto.cs
src/Kindergarten.Core/DTOs/RegisterDto.cs
src/Kindergarten.Core/DTOs/SubscriptionDto.cs
src/Kindergarten.Core/DTOs/TripDto.cs
src/Kindergarten.Core/Entities/ApplicationUser.cs
src/Kindergarten.Core/Entities/Attendance.cs
src/Kindergarten.Core/Entities/Child.cs
src/Kindergarten.Core/Entities/Employee.cs
src/Kindergarten.Core/Entities/LeaveRequest.cs
src/Kindergarten.Core/Entities/Payment.cs
src/Kindergarten.Core/Entities/Permission.cs
src/Kindergarten.Core/Entities/PermissionGroup.cs
src/Kindergarten.Core/Entities/PermissionGroupPermission.cs
src/Kindergarten.Core/Entities/RoleGroup.cs
src/Kindergarten.Core/Entities/RoleGroupPermission.cs
src/Kindergarten.Core/Entities/Subscription.cs
src/Kindergarten.Core/Entities/Tenant.cs
src/Kindergarten.Core/Entities/TripChild.cs
src/Kindergarten.Core/Entities/Trip.cs
src/Kindergarten.Core/Entities/TripLocation.cs
src/Kindergarten.Core/Entities/UserDevice.cs
src/Kindergarten.Core/Entities/UserPermission.cs
src/Kindergarten.Core/Entities/UserPermissionGroup.cs
src/Kindergarten.Core/Entities/UserRoleGroup.cs
src/Kindergarten.Core/Interfaces/IAuthService.cs
src/Kindergarten.Core/Interfaces/IChildService.cs
src/Kindergarten.Core/Interfaces/IEmployeeService.cs
src/Kindergarten.Core/Interfaces/ILeaveRequestService.cs
src/Kindergarten.Core/Interfaces/INotificationService.cs
src/Kindergarten.Core/Interfaces/IPaymentService.cs
src/Kindergarten.Core/Interfaces/ISubscriptionService.cs
src/Kindergarten.Core/Interfaces/ITenantService.cs
src/Kindergarten.Core/Interfaces/ITripService.cs
src/Kindergarten.Infrastructure/Data/ApplicationDbContext.cs
src/Kindergarten.Infrastructure/Services/AuthService.cs
src/Kindergarten.Infrastructure/Services/ChildService.cs
src/Kindergarten.Infrastructure/Services/EmployeeService.cs
src/Kindergarten.Infrastructure/Services/LeaveRequestService.cs
src/Kindergarten.Infrastructure/Services/NotificationService.cs
src/Kindergarten.Infrastructure/Services/PaymentService.cs
src/Kindergarten.Infrastructure/Services/SubscriptionExpiryService.cs
src/Kindergarten.Infrastructure/Services/SubscriptionService.cs
src/Kindergarten.Infrastructure/Services/TenantService.cs
src/Kindergarten.Infrastructure/Services/TripService.cs
src/Kindergarten.Maui/AppShell.xaml.cs
src/Kindergarten.Maui/App.xaml.cs
src/Kindergarten.Maui/Constants.cs
src/Kindergarten.Maui/MauiProgram.cs
src/Kindergarten.Maui/Platforms/Android/MainActivity.cs
src/Kindergarten.Maui/Platforms/Android/MainApplication.cs
src/Kindergarten.Maui/Services/ApiService.cs
src/Kindergarten.Maui/Views/Auth/LoginPage.xaml.cs
src/Kindergarten.Maui/Views/Driver/DriverProfilePage.xaml.cs
src/Kindergarten.Maui/Views/Driver/DriverTripsPage.xaml.cs
src/Kindergarten.Maui/Views/Employee/EmployeeAttendancePage.xaml.cs
src/Kindergarten.Maui/Views/Employee/EmployeeProfilePage.xaml.cs
src/Kindergarten.Maui/Views/Parent/ParentDashboardPage.xaml.cs
src/Kindergarten.Maui/Views/Parent/ProfilePage.xaml.cs
```

### 🗄️ Database Tables
- AspNetUsers (+ TenantId, RoleType, FullName, Address)
- AspNetRoles
- Children (+ TenantId)
- Subscriptions (+ TenantId)
- Payments
- Trips (+ TenantId)
- TripChildren (+ TenantId)
- TripLocations (+ TenantId)
- Employees (+ TenantId)
- Attendance
- UserDevices
- LeaveRequests (+ TenantId)
- Permissions
- UserPermissions
- Tenants
- RoleGroups (+ TenantId)
- RoleGroupPermissions
- UserRoleGroups

### 🔒 Authentication & Security
- JWT Token claims: NameIdentifier, Email, Name, Role, TenantId, RoleGroups
- TenantMiddleware: auto-reads TenantId from JWT
- Global Query Filters: all queries auto-filtered by TenantId
- SuperAdmin: TenantId=0, sees all data
- Impersonation: SuperAdmin can switch to any tenant context

### 🌐 API Controllers
- AuthController: register, login
- ChildrenController: CRUD children
- SubscriptionsController: manage subscriptions
- PaymentsController: manual payments
- TripsController: trips CRUD, GPS, child status, fix-pending
- EmployeesController: attendance, check-in/out, ensure-driver
- UsersController: users CRUD (filtered by TenantId)
- DevicesController: FCM token registration, test notifications
- LeaveRequestsController: submit, review, my-hours
- TenantsController: CRUD, platform-stats, impersonate, create-admin, fix-tenant-data
- PermissionsController: grant/revoke per user, seed
- RoleGroupsController: CRUD groups, assign/unassign users
- NotificationService: SendToDevice, SendToParent, SendToUser, SendToAllParents

### 🖥️ Frontend Files
- /admin.html: Admin dashboard (AR/EN bilingual)
  Pages: Dashboard, Children, Trips, Subscriptions, Payments, 
         Users, Attendance, Leave Requests, Notifications, 
         SignalR, Permissions, Role Groups, Tenants
- /app.html: PWA mobile app (AR/EN bilingual)
  Roles: Parent (home/trips/subs), Driver (trips/GPS), Employee (attendance/leave)
  Features: Side drawer, FCM notifications, SignalR real-time
- /firebase-messaging-sw.js: FCM service worker
- /manifest.json: PWA manifest

### 🔑 Role Hierarchy
- SuperAdmin: platform level, sees all tenants, TenantId=0
- Admin: tenant level, manages own kindergarten
- TenantAdmin: created by SuperAdmin for specific tenant
- Parent: views children + trips
- Driver: manages trips + GPS
- Employee: attendance + leave requests

### 📋 Last Migration
AddTenantIdToTripChildAndLocation

### 💾 Backups
- Git Branch: backup/pre-multitenant-20260523
- Git Tag: v1.0-pre-multitenant
- DB Backup: kmsstorage1779189591/apk/backup-staging-20260523.bacpac


---
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
- SuperAdmin impersonation implemented
- SuperAdmin can manage any tenant
- Data isolation verified per tenant
- Remaining: Unify Role dropdown with Permission Groups
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
