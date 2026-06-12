using System.IdentityModel.Tokens.Jwt;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Kindergarten.Core.Interfaces;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kindergarten.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole>   _roleManager;
    private readonly IConfiguration              _config;
    private readonly ApplicationDbContext        _db;
    private readonly IAuditService               _audit;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole>    roleManager,
        IConfiguration               config,
        ApplicationDbContext         db,
        IAuditService               audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config      = config;
        _db          = db;
        _audit       = audit;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null) return null;

        var user = new ApplicationUser
        {
            FullName    = dto.FullName,
            Email       = dto.Email,
            UserName    = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            RoleType    = "Employee",
            TenantId    = dto.TenantId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return null;

        await _audit.LogAsync("Login", "User", user.Id, $"Login: {user.Email}");
        return await GenerateTokenAsync(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return null;

        var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!valid) return null;

        await _audit.LogAsync("Login", "User", user.Id, $"Login: {user.Email}");
        return await GenerateTokenAsync(user);
    }

    private async Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user)
    {
        var permGroups = await _db.UserPermissionGroups
            .IgnoreQueryFilters()
            .Include(x => x.Group)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        var permissions = permGroups
            .SelectMany(g => g.Group.GroupPermissions)
            .Select(gp => gp.Permission.Name)
            .Distinct()
            .ToList();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email,          user.Email!),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("TenantId", user.TenantId.ToString())
        };

        claims.AddRange(permissions.Select(p => new Claim("Permission", p)));

        foreach (var g in permGroups)
        {
            claims.Add(new Claim("GroupNameEn", g.Group.NameEn));
            claims.Add(new Claim("GroupNameAr", g.Group.NameAr));
        }

        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes((_config["Jwt__Key"] ?? _config["Jwt:Key"])!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddDays(7);

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt__Issuer"] ?? _config["Jwt:Issuer"],
            audience:           _config["Jwt__Audience"] ?? _config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds
        );

        var displayRole = permGroups.Any()
            ? string.Join(",", permGroups.Select(g => g.Group.NameEn))
            : user.RoleType;

        return new AuthResponseDto
        {
            Token    = new JwtSecurityTokenHandler().WriteToken(token),
            UserId   = user.Id,
            Email    = user.Email!,
            FullName = user.FullName,
            Role     = displayRole,
            TenantId = user.TenantId,
            Expiry   = expiry
        };
    }
}
