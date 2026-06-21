namespace Kindergarten.Core.Entities;

public enum NotificationTriggerStatus
{
    Wired,
    Planned
}

public class NotificationKeyInfo
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public List<string> Placeholders { get; set; } = new();
    public string DefaultTitleAr { get; set; } = string.Empty;
    public string DefaultTitleEn { get; set; } = string.Empty;
    public string DefaultBodyAr { get; set; } = string.Empty;
    public string DefaultBodyEn { get; set; } = string.Empty;
    public NotificationTriggerStatus Status { get; set; } = NotificationTriggerStatus.Wired;
}

public static class NotificationRegistry
{
    public static readonly List<NotificationKeyInfo> All = new()
    {
        new NotificationKeyInfo
        {
            Key = "payment_confirmed",
            Category = "Payments",
            DescriptionAr = "إشعار لولي الأمر عند تسجيل دفعة جديدة",
            DescriptionEn = "Notifies the parent when a payment is recorded",
            Placeholders = new() { "amount", "childName" },
            DefaultTitleAr = "تم تأكيد الدفع ✅",
            DefaultTitleEn = "Payment Confirmed ✅",
            DefaultBodyAr = "تم استلام دفعة بمبلغ {amount} ريال لاشتراك {childName}",
            DefaultBodyEn = "Payment of {amount} SAR received for {childName}'s subscription",
            Status = NotificationTriggerStatus.Wired
        },
 new NotificationKeyInfo
        {
            Key = "subscription_created",
            Category = "Subscriptions",
            DescriptionAr = "إشعار لولي الأمر عند تسجيل اشتراك جديد",
            DescriptionEn = "Notifies the parent when a new subscription is created",
            Placeholders = new() { "type", "childName", "price" },
            DefaultTitleAr = "تم تسجيل اشتراك جديد",
            DefaultTitleEn = "New Subscription Registered",
            DefaultBodyAr = "تم تسجيل اشتراك {type} لـ {childName} بقيمة {price} ريال",
            DefaultBodyEn = "A {type} subscription has been registered for {childName} for {price} SAR",
            Status = NotificationTriggerStatus.Wired
        },
new NotificationKeyInfo
        {
            Key = "subscription_cancelled",
            Category = "Subscriptions",
            DescriptionAr = "إشعار لولي الأمر عند إلغاء اشتراك",
            DescriptionEn = "Notifies the parent when a subscription is cancelled",
            Placeholders = new() { "type", "childName" },
            DefaultTitleAr = "تم إلغاء الاشتراك",
            DefaultTitleEn = "Subscription Cancelled",
            DefaultBodyAr = "تم إلغاء اشتراك {type} الخاص بـ {childName}",
            DefaultBodyEn = "The {type} subscription for {childName} has been cancelled",
            Status = NotificationTriggerStatus.Wired
        },
 new NotificationKeyInfo
        {
            Key = "leave_request_submitted",
            Category = "LeaveRequests",
            DescriptionAr = "إشعار للمعتمدين عند تقديم طلب إذن جديد",
            DescriptionEn = "Notifies approvers when a new leave request is submitted",
            Placeholders = new() { "employeeName", "hours" },
            DefaultTitleAr = "طلب إذن جديد",
            DefaultTitleEn = "New Leave Request",
            DefaultBodyAr = "{employeeName} طلب إذناً لمدة {hours} ساعة",
            DefaultBodyEn = "{employeeName} requested {hours}h leave",
            Status = NotificationTriggerStatus.Wired
        },
new NotificationKeyInfo
        {
            Key = "leave_request_reviewed",
            Category = "LeaveRequests",
            DescriptionAr = "إشعار للموظف عند مراجعة طلب الإذن",
            DescriptionEn = "Notifies the employee when their leave request is reviewed",
            Placeholders = new() { "statusAr", "statusEn" },
            DefaultTitleAr = "تحديث حالة طلب الإذن",
            DefaultTitleEn = "Leave Request Update",
            DefaultBodyAr = "تم {statusAr} طلب الإذن الخاص بك",
            DefaultBodyEn = "Your leave request has been {statusEn}",
            Status = NotificationTriggerStatus.Wired
        },
        new NotificationKeyInfo
        {
            Key = "trip_started",
            Category = "Trips",
            DescriptionAr = "إشعار لأولياء أمور أطفال الرحلة عند بدء الرحلة",
            DescriptionEn = "Notifies parents of children on the trip when it starts",
            Placeholders = new(),
            DefaultTitleAr = "بدأت الرحلة",
            DefaultTitleEn = "Trip Started",
            DefaultBodyAr = "السائق في الطريق إليكم",
            DefaultBodyEn = "The driver is on the way",
            Status = NotificationTriggerStatus.Wired
        },
 new NotificationKeyInfo
        {
            Key = "trip_ended",
            Category = "Trips",
            DescriptionAr = "إشعار لأولياء أمور أطفال الرحلة عند انتهاء الرحلة",
            DescriptionEn = "Notifies parents of children on the trip when it ends",
            Placeholders = new(),
            DefaultTitleAr = "انتهت الرحلة",
            DefaultTitleEn = "Trip Completed",
            DefaultBodyAr = "تمت الرحلة بنجاح",
            DefaultBodyEn = "The trip has been completed successfully",
            Status = NotificationTriggerStatus.Wired
        },
        new NotificationKeyInfo
        {
            Key = "child_registered",
            Category = "Children",
            DescriptionAr = "إشعار لولي الأمر عند تسجيل طفل جديد",
            DescriptionEn = "Notifies the parent when a new child is registered",
            Placeholders = new() { "childName" },
            DefaultTitleAr = "تم تسجيل طفلك",
            DefaultTitleEn = "Child Registered",
            DefaultBodyAr = "تم تسجيل {childName} بنجاح في الروضة",
            DefaultBodyEn = "{childName} has been successfully registered at the kindergarten",
            Status = NotificationTriggerStatus.Wired
        },
new NotificationKeyInfo
        {
            Key = "attendance_marked",
            Category = "Attendance",
            DescriptionAr = "إشعار لولي الأمر عند تسجيل حالة حضور الطفل (Note 55)",
            DescriptionEn = "Notifies the parent when child attendance status is recorded (pending - Note 55)",
            Placeholders = new() { "childName", "status" },
            DefaultTitleAr = "تحديث حالة الحضور",
            DefaultTitleEn = "Attendance Status Update",
            DefaultBodyAr = "تم تسجيل حالة طفلك {childName}: {status}",
            DefaultBodyEn = "Your child {childName}'s attendance status: {status}",
            Status = NotificationTriggerStatus.Planned
        }
    };
public static NotificationKeyInfo? Find(string key) =>
        All.FirstOrDefault(k => k.Key == key);

    public static (string titleAr, string titleEn, string bodyAr, string bodyEn) GetDefaults(string key)
    {
        var info = Find(key);
        if (info == null)
            return ("إشعار", "Notification", "{message}", "{message}");

        return (info.DefaultTitleAr, info.DefaultTitleEn, info.DefaultBodyAr, info.DefaultBodyEn);
    }
}
