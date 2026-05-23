using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kindergarten.Core.DTOs;
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

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole>    roleManager,
        IConfiguration               config)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config      = config;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        // Check if user exists
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null) return null;

        // Create user
        var user = new ApplicationUser
        {
            FullName    = dto.FullName,
            Email       = dto.Email,
            UserName    = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            RoleType    = dto.RoleType
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return null;

        // Create role if not exists and assign
        if (!await _roleManager.RoleExistsAsync(dto.RoleType))
            await _roleManager.CreateAsync(new IdentityRole(dto.RoleType));

        await _userManager.AddToRoleAsync(user, dto.RoleType);

        return await GenerateTokenAsync(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return null;

        var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!valid) return null;

        return await GenerateTokenAsync(user);
    }

    private async Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? user.RoleType;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email,          user.Email!),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim(ClaimTypes.Role,           role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("TenantId", user.TenantId.ToString())
        };

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes((_config["Jwt__Key"] ?? _config["Jwt:Key"])!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = DateTime.UtcNow.AddDays(7);

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt__Issuer"] ?? _config["Jwt:Issuer"],
            audience:           _config["Jwt__Audience"] ?? _config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds
        );

        return new AuthResponseDto
        {
            Token    = new JwtSecurityTokenHandler().WriteToken(token),
            UserId   = user.Id,
            Email    = user.Email!,
            FullName = user.FullName,
            Role     = role,
            TenantId = user.TenantId,
            Expiry   = expiry
        };
    }
}
