# KMS Notification System — Handoff Documentation

**Last Updated:** 2026-06-21 (Phase 4 complete)
**Status:** All 7 actionable notification trigger keys wired, tested, and deployed. Admin UI fully functional with dynamic template registry.

---

## Current State (as of 2026-06-21)

### ✅ Complete Features (all deployed to prod)

**Notification triggers wired (7/9 keys):**
- `payment_confirmed` — confirmed working
- `subscription_created` — confirmed working  
- `subscription_cancelled` — wired this session
- `leave_request_submitted` — wired + bug fixed (was broadcasting to all parents, now correctly targets Leave.Approve holders)
- `leave_request_reviewed` — newly wired (notifies employee of approve/reject)
- `trip_started` — wired + critical bug fixed (scoping)
- `trip_ended` — wired + critical bug fixed (scoping)
- `child_registered` — wired + ParentName lookup bug fixed

**Registry infrastructure:**
- `NotificationKeyInfo.cs`: Single source of truth defining all 9 keys (key, category, AR/EN descriptions, placeholders, defaults, status)
- `NotificationRegistry` static class: Contains `All` list and helper methods (`Find`, `GetDefaults`)
- `GET /api/notification-templates/registry` endpoint: Returns full registry merged with per-tenant custom-override status
- `NotificationService.DefaultTemplates.Get()`: Now delegates to registry, eliminating duplication

**Admin UI:**
- `admin.html` notification templates dropdown: Fully dynamic, populated from registry endpoint
- Shows all 9 keys with AR descriptions, placeholders, status icons (⏳ Planned, ✅ Custom)
- `loadNotificationRegistry()` function: Fetches & caches registry data, populates dropdown
- Frontend bug fixed: `loadDevices()` early return was blocking template loader for accounts with zero FCM devices

### ⏳ Planned Triggers (2/9 keys)
- `attendance_marked` — blocked on Note 55 (child attendance feature doesn't exist yet)

---

## Known Issues & Gaps

**FCM device registration gap (Note 48):**
- Most accounts have zero registered devices
- `loadNotificationRegistry()` is now unconditionally called (fixed in Phase 4), so the templates dropdown works even without devices
- Real push notifications still won't reach parents without device registration, but that's a separate, pre-existing issue

**Unblocked by this work:**
- Exam/academic results viewing
- Direct messaging with teachers/admin

---

## Architecture Reference

**Permission-based notification targeting:**
- Use dedicated permission queries (e.g., `GetUserIdsWithPermissionAsync`) instead of role strings
- Example: `leave_request_submitted` queries for `Leave.Approve` holders, falls back to `RoleType=="Admin"`
- All permission checks embedded in JWT at login; stateless authorization

**Notification scoping patterns (critical bugs fixed in Phase 4):**
- **leave_request_submitted**: was `SendToAllParentsAsync` (wrong), now targets `Leave.Approve` holders
- **trip_started/trip_ended**: were `SendToAllParentsAsync` (wrong), now filter to `parents of children on the specific trip` only
- Pattern: Always narrow recipient lists to the specific context (permission, resource relationship, etc.), never tenant-wide broadcasts

**Template system:**
- `NotificationTemplate` entity: Per-tenant custom overrides (AR/EN title+body)
- `NotificationRegistry`: Built-in defaults (code-defined, same for all tenants)
- Rendering: `SendTemplatedAsync(key, userId, placeholders, metadata)` looks up custom template if exists, falls back to registry default, substitutes placeholders

**Frontend template loading flow:**
1. User navigates to الإشعارات
2. `showPage('notifications')` → `loaders['notifications']()` → `loadDevices()`
3. `loadDevices()` calls `loadNotificationRegistry()` (unconditionally, even if devices list is empty)
4. `loadNotificationRegistry()` fetches `/api/notification-templates/registry`, populates dropdown, caches data
5. User selects a key → `loadTemplateForKey()` pulls defaults from cache, allows editing

---
## Key Files

**Backend:**
- `src/Kindergarten.Core/Entities/NotificationKeyInfo.cs` — registry definition
- `src/Kindergarten.Infrastructure/Services/NotificationService.cs` — `SendTemplatedAsync`, `DefaultTemplates` (now delegating)
- `src/Kindergarten.Api/Controllers/NotificationTemplatesController.cs` — `GET /registry` endpoint
- `src/Kindergarten.Infrastructure/Services/LeaveRequestService.cs` — has `GetUserIdsWithPermissionAsync` helper (reusable pattern)

**Frontend:**
- `src/Kindergarten.Api/wwwroot/admin.html` — lines ~2316 (`loadDevices`), ~2327 (`loadNotificationRegistry`), ~2340 (`loadTemplateForKey`), ~973 (`<select id="tplKey">`)

**Documentation:**
- `KMS_NOTES_FULL.md` — comprehensive session-by-session history (this file is the source of truth)
- `NOTIFICATIONS_MODULE_HANDOFF.md` — this file (next-session quick reference)

---

## Testing Checklist for Next Work



When adding new features or modifying notification logic:
- ✅ Verify via `dotnet build` (backend changes)
- ✅ Verify via `node --check` on admin.html (frontend changes)
- ✅ Test on staging first with real tenant data (create test records, verify endpoints return correct shapes)
- ✅ Browser test on prod admin UI (navigate to الإشعارات, confirm dropdown populates, select keys, verify form fields fill correctly)
- ✅ Clean up test data after verification (direct SQL or API delete)
- ✅ Verify git history is clean (small, focused commits with clear messages)

---

## Next Session Priorities

1. **Note 55 — Attendance marking & notification:** Child attendance feature needs to exist first (admin marking status, optional custom message). Wire `attendance_marked` trigger once that's ready.
2. **Note 26 — Granular CRUD permissions:** Currently some endpoints use basic role checks instead of fine-grained permissions. Standardize via `RequirePermission` attribute.
3. **Note 9 Phase 2 — Moyasar payment integration:** Currently only manual payments. Integrate Moyasar gateway for real-time automated payments.

---
## Debugging Reference (Common Issues This Session)

**"Dropdown is empty":**
- Check Network tab → does `/api/notification-templates/registry` request appear? If no, `loadNotificationRegistry()` never ran.
- Check browser Console → any JS errors? Silent failures in `api()` helper return `null` with no error logged.
- Check if navigating to الإشعارات actually fires `loadDevices()` (confirmed via Network tab for `/api/devices` first request).
- Remember: early `return` in `loadDevices()` if no devices exist was the root cause in Phase 4 — now fixed.

**"Registry endpoint returns 403":**
- Fresh login via curl to confirm token has `Notifications.Send` permission (decode JWT payload).
- Browser stale token: log out fully, close tab, reopen, fresh login.

**"Template text shows literal {placeholders} instead of real values":**
- Dropdown showing `{childName},{amount}` in labels is expected (admin documentation)
- Template form fields showing `{placeholders}` is expected (editable defaults)
- Actual notification at runtime not substituting? Check `SendTemplatedAsync` placeholder dict in the trigger code (should have real values before calling `SendTemplatedAsync`)

**Shell quoting issues with `!` character:**
- Heredoc with `'NOEXPAND'` quoted delimiter prevents history expansion
- If still broken, use base64 encoding: encode script → decode in shell → execute
- Example: `echo "..." | base64 -d > /tmp/script.py && python3 /tmp/script.py`

