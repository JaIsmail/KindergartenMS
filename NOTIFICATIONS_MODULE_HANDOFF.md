# Notifications Module — Handoff Document (2026-06-20)

## Current State (Working, Deployed)

### Backend Infrastructure (✅ Done & Deployed)
- **NotificationTemplate entity** (`src/Kindergarten.Core/Entities/NotificationTemplate.cs`): Id, Key, TitleAr, TitleEn, BodyAr, BodyEn, TenantId, IsActive
- **Table**: `NotificationTemplates` — created via startup SQL in Program.cs (IF NOT EXISTS pattern)
- **INotificationService.SendTemplatedAsync(key, userId, replacements, data)** — looks up a custom template by key+tenant, falls back to hardcoded default if none exists, substitutes `{placeholder}` tokens, sends via existing `SendToUserAsync`
- **DefaultTemplates.Get(key)** (static class in `NotificationService.cs`) — currently a simple `switch` statement with 3 hardcoded keys: `payment_confirmed`, `subscription_created`, `attendance_marked` (this last one has NO actual trigger wired yet — see below)
- **NotificationTemplatesController** (`api/notification-templates`): GET (list), GET/{key}, PUT/{key} (upsert), DELETE/{key} (reset to default) — all require `Notifications.Send` permission, tenant-scoped

### Currently Wired Triggers (✅ Done)
1. **`payment_confirmed`** — fires in `PaymentService.CreateAsync`, after a payment is recorded. Placeholders: `{amount}`, `{childName}`
2. **`subscription_created`** — fires in `SubscriptionService.CreateAsync`, when a new subscription is created. Placeholders: `{type}`, `{childName}`, `{price}`

### Admin UI (✅ Done & Deployed)
- New card "✏️ نصوص الإشعارات" added to **الإشعارات** page in admin.html (under existing device-list card)
- Dropdown currently HARDCODED with 3 options matching the backend keys above (NOT dynamic yet — see "In Progress" below)
- Edit title/body AR+EN, Save (PUT), Reset to Default (DELETE)
- Tested end-to-end on prod: custom template saves, payment flow still works cleanly with templated call

## In Progress / Abandoned Mid-Edit (⚠️ Needs Attention)
- Started building a richer `NotificationKeyInfo` registry class (key, category, description AR/EN, placeholders list, default text) to replace the simple switch statement, intended to power a NEW endpoint (`GET /api/notification-templates/registry` or similar) so the admin UI dropdown could be **dynamic** instead of hardcoded
- **This edit was NEVER applied** — multiple `str_replace` attempts failed due to whitespace/encoding mismatches (Arabic literal vs `\u` escape sequences caused several failed match attempts)
- Current code is back to the simple, working switch-statement version — nothing is broken, but the dynamic-registry goal is unrealized
- If resuming: rebuild this cleanly, ideally writing the new file content via `create_file`/`str_replace` against a freshly-viewed copy of the file rather than guessing at exact whitespace

## Key Design Decision Made This Session
**User wants TRUE admin-defined notification types** — not just text editing for developer-defined triggers, but the ability for admins to create their own named notification templates.

Agreed approach: **scan codebase for all realistic trigger points first, register them all as system triggers in code, THEN admins create named templates per trigger** (not fully free-form custom triggers — those require actual code-level events to fire from).

## Full Trigger Inventory Found This Session (NOT all wired yet)
From scanning all `[HttpPost]`/`[HttpPut]`/`[HttpDelete]` actions across controllers:

| Controller | Action | Potential Notification? | Status |
|---|---|---|---|
| PaymentsController | Create | payment_confirmed | ✅ Wired |
| SubscriptionsController | Create | subscription_created | ✅ Wired |
| SubscriptionsController | Delete | subscription_cancelled? | Not discussed |
| LeaveRequestsController | Create | (admin notified — `SendToAllParentsAsync`, NOT templated, separate legacy code path) | Not migrated to template system |
| LeaveRequestsController | Review (approve/reject) | leave_request_reviewed (notify employee) | Not discussed |
| TripsController | Start | trip_started (notify parent) | Not discussed — mentioned in old Note 1 but never implemented |
| TripsController | End | trip_ended (notify parent) | Not discussed |
| ChildrenController | Create | child_registered? | Not discussed |
| EmployeesController | checkin/checkout | STAFF attendance — NOT child attendance, clarified this session as unrelated to Note 55 |

## Note 55 — Child Attendance Notification — SCOPE CLARIFIED THIS SESSION
**Critical finding**: there is currently NO child attendance (present/absent) tracking feature in the system at all. `EmployeesController`'s checkin/checkout is STAFF attendance (teachers, drivers, etc. clocking in), completely separate and unrelated.

Note 55 therefore requires building, from scratch:
1. A new entity/table for child daily attendance status (present/absent), likely: ChildId, Date, Status, MarkedBy, TenantId
2. A new controller/endpoint for staff to mark a child's status (manually, not auto-derived from anything currently existing)
3. THEN wire `attendance_marked` template trigger to fire when that status is recorded
4. Admin UI: a way to mark attendance (likely a new page, or extend the existing staff Attendance page) with status selection
5. The notification itself (once attendance recording exists) is now straightforward given the templates infrastructure already built

**This is a multi-step feature, not just "add a notification call."**

## Other Notes Logged This Session (separate from notifications, for context)
- **Note 54**: Account dropdown menu needed (full name, avatar placeholder, change password, sign out) — top-left avatar in admin.html/app.html headers
- **EmployeesController naming**: cosmetic — name is legacy, no longer implies generic "Employee" role since RoleType isn't used for authorization; actual roles are Teacher/Accountant/Supervisor/etc via PermissionGroups. Could rename to AttendanceController/StaffController later, non-blocking.

## Recommended Starting Point for Next Session
1. Re-verify current deployed state matches this document (grep checks above)
2. Decide: build the dynamic registry properly now, or ship the 3 working hardcoded triggers as "v1" and treat registry expansion as its own follow-up
3. If tackling Note 55: design the child-attendance entity/table/UI FIRST as a standalone feature, THEN wire its notification trigger using the existing SendTemplatedAsync infrastructure (no new notification plumbing needed, just a new key + trigger call site)
4. Consider migrating LeaveRequestsController's notification (currently uses raw SendToAllParentsAsync, not templated) to the template system for consistency
