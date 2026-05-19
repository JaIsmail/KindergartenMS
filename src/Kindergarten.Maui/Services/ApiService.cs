using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Kindergarten.Maui.Services;

public class ApiService
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService()
    {
#if ANDROID
        var handler = new Xamarin.Android.Net.AndroidMessageHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _client = new HttpClient(handler)
#else
        _client = new HttpClient()
#endif
        {
            BaseAddress = new Uri(Constants.ApiBaseUrl),
            Timeout     = TimeSpan.FromSeconds(30)
        };
    }

    private void SetAuthHeader()
    {
        var token = SecureStorage.GetAsync(Constants.TokenKey).Result;
        if (!string.IsNullOrEmpty(token))
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    private void SetLangHeader()
    {
        var lang = Preferences.Get(Constants.LangKey, "ar");
        _client.DefaultRequestHeaders.Remove("Accept-Language");
        _client.DefaultRequestHeaders.Add("Accept-Language", lang);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        SetAuthHeader(); SetLangHeader();
        var response = await _client.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object body)
    {
        SetAuthHeader(); SetLangHeader();
        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(endpoint, content);
        if (!response.IsSuccessStatusCode) return default;
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, _json);
    }

    public async Task<T?> PutAsync<T>(string endpoint)
    {
        SetAuthHeader(); SetLangHeader();
        var response = await _client.PutAsync(endpoint, null);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    // ── Auth ──────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(string email, string password)
        => await PostAsync<LoginResponse>("/api/auth/login", new { email, password });

    public async Task<LoginResponse?> RegisterAsync(string fullName, string email, string password, string phone, string roleType)
        => await PostAsync<LoginResponse>("/api/auth/register", new { fullName, email, password, phoneNumber = phone, roleType });

    // ── Children ──────────────────────────────────────
    public async Task<List<ChildResponse>?> GetChildrenAsync()
        => await GetAsync<List<ChildResponse>>("/api/children");

    public async Task<ChildResponse?> CreateChildAsync(string name, DateTime birthDate, string cls, string healthNotes)
        => await PostAsync<ChildResponse>("/api/children", new { name, birthDate, @class = cls, healthNotes });

    // ── Subscriptions ─────────────────────────────────
    public async Task<List<SubscriptionResponse>?> GetSubscriptionsAsync()
        => await GetAsync<List<SubscriptionResponse>>("/api/subscriptions");

    // ── Trips ─────────────────────────────────────────
    public async Task<List<TripResponse>?> GetMyTripsAsync()
        => await GetAsync<List<TripResponse>>("/api/trips/driver");

    public async Task<TripResponse?> StartTripAsync(int tripId)
        => await PutAsync<TripResponse>($"/api/trips/{tripId}/start");

    public async Task<TripResponse?> EndTripAsync(int tripId)
        => await PutAsync<TripResponse>($"/api/trips/{tripId}/end");

    public async Task UpdateChildStatusAsync(int tripId, int childId, string status, string type)
        => await PostAsync<object>("/api/trips/child-status", new { tripId, childId, status, type });

    public async Task UpdateLocationAsync(int tripId, double latitude, double longitude)
        => await PostAsync<object>("/api/trips/location", new { tripId, latitude, longitude });

    // ── Employees ─────────────────────────────────────
    public async Task<AttendanceResponse?> CheckInAsync()
        => await PostAsync<AttendanceResponse>("/api/employees/checkin", new { biometricVerified = true });

    public async Task<AttendanceResponse?> CheckOutAsync()
        => await PostAsync<AttendanceResponse>("/api/employees/checkout", new { });

    public async Task<List<AttendanceResponse>?> GetMyAttendanceAsync()
        => await GetAsync<List<AttendanceResponse>>("/api/employees/my-attendance");

    // ── Devices ───────────────────────────────────────
    public async Task RegisterDeviceAsync(string deviceToken, string platform = "Android")
        => await PostAsync<object>("/api/devices/register", new { deviceToken, platform });
}

// ── Response Models ───────────────────────────────────
public class LoginResponse
{
    public string Token    { get; set; } = string.Empty;
    public string UserId   { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
}

public class ChildResponse
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public DateTime BirthDate   { get; set; }
    public string   Class       { get; set; } = string.Empty;
    public string   HealthNotes { get; set; } = string.Empty;
    public string   ParentName  { get; set; } = string.Empty;
}

public class SubscriptionResponse
{
    public int      Id            { get; set; }
    public string   ChildName     { get; set; } = string.Empty;
    public string   Type          { get; set; } = string.Empty;
    public decimal  Price         { get; set; }
    public DateTime StartDate     { get; set; }
    public DateTime EndDate       { get; set; }
    public string   PaymentStatus { get; set; } = string.Empty;
}

public class TripResponse
{
    public int                   Id         { get; set; }
    public string                DriverName { get; set; } = string.Empty;
    public string                Direction  { get; set; } = string.Empty;
    public string                Status     { get; set; } = string.Empty;
    public DateTime              Date       { get; set; }
    public DateTime?             StartTime  { get; set; }
    public DateTime?             EndTime    { get; set; }
    public List<TripChildStatus> Children   { get; set; } = new();
}

public class TripChildStatus
{
    public int     ChildId       { get; set; }
    public string  ChildName     { get; set; } = string.Empty;
    public string  PickupStatus  { get; set; } = string.Empty;
    public string  DropoffStatus { get; set; } = string.Empty;
}

public class AttendanceResponse
{
    public int       Id           { get; set; }
    public DateTime  Date         { get; set; }
    public DateTime? CheckInTime  { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string    Status       { get; set; } = string.Empty;
    public string?   WorkingHours { get; set; }
}
