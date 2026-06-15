using System.Text;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Kindergarten.Api.Authorization;
using Kindergarten.Api.Hubs;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ── CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));


// ── JWT Authentication
var jwtKey = builder.Configuration["Jwt__Key"]
          ?? builder.Configuration["Jwt:Key"]
          ?? "KMS@JwtSecretKey#2025Kindergarten!Secure32Chars";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = "KindergartenApi",
        ValidAudience            = "KindergartenApp",
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ── Authorization Policies
builder.Services.AddTransient<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization(options =>
{
    var permissions = new[]
    {
        // Children
        "Children.View","Children.Add","Children.Edit","Children.Delete",
        // Subscriptions
        "Subscriptions.View","Subscriptions.Add","Subscriptions.Edit","Subscriptions.Delete",
        // Payments
        "Payments.View","Payments.Add","Payments.Delete",
        // Users
        "Users.View","Users.Add","Users.Edit","Users.Delete",
        // Attendance
        "Attendance.CheckIn","Attendance.ViewOwn","Attendance.ViewAll",
        // Leave
        "Leave.Submit","Leave.ViewAll","Leave.Approve",
        // Trips
        "Trips.View","Trips.Manage","Trips.Track",
        // Lists
        "Lists.View","Lists.Manage",
        // Permissions
        "Permissions.View","Permissions.Edit",
        "Groups.View","Groups.Add","Groups.Edit","Groups.Delete","Groups.Assign",
        // Tenants
        "Tenants.View","Tenants.Add","Tenants.Edit","Tenants.Update",
        // Reports
        "Reports.View","Reports.Export",
        // Settings
        "Settings.View","Settings.Edit",
        // Discounts
        "Discounts.View","Discounts.Manage","Discounts.Apply",
        // Notifications & Finance
        "Notifications.Send","Finance.ViewAll","AuditLog.View"
    };
    foreach (var permission in permissions)
        options.AddPolicy(permission,
            policy => policy.Requirements.Add(new PermissionRequirement(permission)));
});

// ── Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<Kindergarten.Core.Interfaces.IAuditService, Kindergarten.Infrastructure.Services.AuditService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<Kindergarten.Infrastructure.Services.SubscriptionExpiryService>();
builder.Services.AddScoped<Kindergarten.Core.Interfaces.ILeaveRequestService, Kindergarten.Infrastructure.Services.LeaveRequestService>();
builder.Services.AddHostedService<Kindergarten.Infrastructure.Services.SubscriptionExpiryService>();
builder.Services.AddScoped<Kindergarten.Core.Interfaces.ILeaveRequestService, Kindergarten.Infrastructure.Services.LeaveRequestService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IChildService, ChildService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ── Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseMiddleware<Kindergarten.Api.Middleware.TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => new {
    status  = "healthy",
    version = "2.0.0",
    env     = app.Environment.EnvironmentName,
    time    = DateTime.UtcNow
});

// ── DB Test endpoint
app.MapGet("/dbtest", async (ApplicationDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var userCount  = canConnect ? await db.Users.CountAsync() : -1;
        return Results.Ok(new {
            canConnect,
            userCount,
            database = db.Database.GetDbConnection().Database,
            server   = db.Database.GetDbConnection().DataSource
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapHub<TripHub>("/hubs/trip");


// ── Admin Dashboard


// Seed SuperAdmin
using (var scope = app.Services.CreateScope())
{
    var db0 = scope.ServiceProvider.GetRequiredService<Kindergarten.Infrastructure.Data.ApplicationDbContext>();
    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Kindergarten.Core.Entities.ApplicationUser>();

    // Create SuperAdmin user if not exists
    var superAdminEmail = "superadmin@kms-platform.com";
    var superAdmin = await db0.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == superAdminEmail);
    if (superAdmin == null)
    {
        superAdmin = new Kindergarten.Core.Entities.ApplicationUser
        {
            UserName = superAdminEmail,
            Email    = superAdminEmail,
            FullName = "Super Admin",
            RoleType = "SuperAdmin",
            TenantId = 0 // SuperAdmin belongs to no tenant
        };
        superAdmin.PasswordHash = hasher.HashPassword(superAdmin, "SuperAdmin@123456");
        db0.Users.Add(superAdmin);
        await db0.SaveChangesAsync();
    }
}

// Create AuditLogs table if not exists
using (var scope2 = app.Services.CreateScope())
{
    var db2 = scope2.ServiceProvider.GetRequiredService<Kindergarten.Infrastructure.Data.ApplicationDbContext>();
    try
    {
        db2.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
            CREATE TABLE AuditLogs (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                TenantId INT NOT NULL DEFAULT 0,
                UserId NVARCHAR(450) NOT NULL DEFAULT '',
                UserEmail NVARCHAR(256) NOT NULL DEFAULT '',
                UserName NVARCHAR(256) NOT NULL DEFAULT '',
                Action NVARCHAR(100) NOT NULL DEFAULT '',
                EntityType NVARCHAR(100) NOT NULL DEFAULT '',
                EntityId NVARCHAR(100) NOT NULL DEFAULT '',
                Details NVARCHAR(MAX) NOT NULL DEFAULT '',
                IpAddress NVARCHAR(50) NOT NULL DEFAULT '',
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            )
        ");
    }
    catch { }
}
app.Run();
