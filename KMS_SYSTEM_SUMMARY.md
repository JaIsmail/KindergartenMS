# KMS System Summary — For Chat Transfer
Generated: 2026-05-30

## Permission System (How It Works)

### Flow
1. Admin creates PermissionGroup (e.g. "Teacher") with specific permissions
2. Admin assigns user to PermissionGroup via Admin dashboard
3. On login → AuthService loads user's groups + all permissions from groups
4. JWT token includes:
   - `role` claim = group name(s) comma-separated (e.g. "Teacher")
   - `Permission` claim = individual claims for each permission (e.g. "SubmitLeaveRequest")
5. API endpoints use `[RequirePermission("XYZ")]` attribute
6. PermissionHandler checks JWT Permission claims first (fast, no DB)
7. Falls back to DB check via UserPermissions + UserPermissionGroups tables

### Permission Groups (Current)
- Admin (id=27): 20 permissions (all)
- Driver (id=28): 3 permissions (ManageTrips, TrackTrips, ViewOwnAttendance)
- Parent (id=29): 4 permissions (ViewChildren, TrackTrips, ViewSubscriptions, ViewFinancials)
- Teacher (id=30): 8 permissions
- Accountant (id=31): 9 permissions
- Supervisor (id=32): 0 permissions (needs setup)

### Available Permissions (20 total)
ManageUsers, ViewUsers, ManageChildren, ViewChildren,
ManageTrips, TrackTrips, ManageSubscriptions, ViewSubscriptions,
ManagePayments, ViewFinancials, ManageAttendance, ViewOwnAttendance,
SubmitLeaveRequest, ManageLeaveRequests, ManagePermissions, ManageRoleGroups,
ManageTenants, UpdateTenant, SendNotifications, ViewReports

### Key Tables
- AspNetUsers: all users (teachers, drivers, parents, admins)
- PermissionGroups: group definitions (Admin, Teacher, Driver, etc.)
- PermissionGroupPermissions: which permissions each group has
- UserPermissionGroups: which groups each user belongs to
- UserPermissions: direct permission grants (bypass groups)

### Key Files
- AuthService: generates JWT with permission claims
- PermissionHandler: validates permissions on each request
- RequirePermissionAttribute: applies permission check to endpoints
- Program.cs: registers all 20 permission policies

## Completed Features
- ✅ Multi-tenant SaaS (Note 8)
- ✅ Permission Groups system (Note 6, 20)
- ✅ Attendance with multiple periods + geo restriction (Note 2)
- ✅ Leave requests (Note 31)
- ✅ Settings page (Note 27)
- ✅ Dynamic lists (Note 3)
- ✅ Payment system Phase 1 (Note 9)
- ✅ Tenant management

## Pending Features
- 🟠 Payment Phase 2: Moyasar integration
- 🟠 Granular CRUD permissions (Note 26)
- 🟡 Note 1: Test notification cycle
- 🟡 Note 4: Profile picture + password update
- 🟡 Note 7: Flexible roles (finish)
- 🟡 Note 11: Group permissions pages
- 🟡 Note 22: Additional user fields
- 🟡 Note 25: Settings map integration
- 🟡 Note 28: Remove old RoleGroups system
- 🟡 Note 29: Delete functions + audit log
- 🟡 Note 30: KSA timezone
- 🟡 Note 32: Leave request details modal fix
- 🟢 Notes 12-17: MAUI fixes

## Tech Stack
- Backend: ASP.NET Core 9
- DB: SQL Server (Azure)
- Frontend: Vanilla JS PWA + Admin HTML
- Mobile: .NET MAUI Android
- Auth: JWT + ASP.NET Identity
- Notifications: Firebase FCM
- Real-time: SignalR
- Maps: Leaflet.js
- CI/CD: GitHub Actions → Azure App Service

## Test Accounts
- SuperAdmin: superadmin@kms-platform.com / SuperAdmin@123456
- Admin: admin@kms-staging.com / Admin@123456
- Teacher: teacher@kms-staging.com / Teacher@123456
- Driver: Driver1@kms-staging.com / Driver@123456
- Parent: parent.test@kms.com / Parent@123456

## Key URLs
- API: https://kms-api-staging-kg01.azurewebsites.net
- Admin: https://kms-api-staging-kg01.azurewebsites.net/admin.html
- PWA: https://kms-api-staging-kg01.azurewebsites.net/app.html
- Repo: github.com/JaIsmail/KindergartenMS
