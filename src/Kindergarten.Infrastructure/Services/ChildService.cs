using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class ChildService : IChildService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    public ChildService(ApplicationDbContext db, ITenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<IEnumerable<ChildResponseDto>> GetAllAsync(string parentId)
    {
        return await _db.Children
            .Include(c => c.Parent)
            .Where(c => c.ParentId == parentId)
            .Select(c => new ChildResponseDto
            {
                Id          = c.Id,
                Name        = c.Name,
                BirthDate   = c.BirthDate,
                Class       = c.Class,
                HealthNotes = c.HealthNotes,
                ParentId    = c.ParentId,
                ParentName  = c.Parent.FullName
            })
            .ToListAsync();
    }

    public async Task<ChildResponseDto?> GetByIdAsync(int id, string parentId)
    {
        var c = await _db.Children
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == id && c.ParentId == parentId);

        if (c == null) return null;

        return new ChildResponseDto
        {
            Id          = c.Id,
            Name        = c.Name,
            BirthDate   = c.BirthDate,
            Class       = c.Class,
            HealthNotes = c.HealthNotes,
            ParentId    = c.ParentId,
            ParentName  = c.Parent.FullName
        };
    }

    public async Task<ChildResponseDto> CreateAsync(CreateChildDto dto, string parentId)
    {
        var child = new Child
        {
            Name        = dto.Name,
            BirthDate   = dto.BirthDate,
            Class       = dto.Class,
            HealthNotes = dto.HealthNotes,
            ParentId    = parentId,
            TenantId    = _tenantService.GetTenantId()
        };

        _db.Children.Add(child);
        await _db.SaveChangesAsync();

        var parent = await _db.Users.FindAsync(parentId);

        return new ChildResponseDto
        {
            Id          = child.Id,
            Name        = child.Name,
            BirthDate   = child.BirthDate,
            Class       = child.Class,
            HealthNotes = child.HealthNotes,
            ParentId    = child.ParentId,
            ParentName  = parent?.FullName ?? string.Empty
        };
    }

    public async Task<bool> DeleteAsync(int id, string parentId)
    {
        var child = await _db.Children
            .FirstOrDefaultAsync(c => c.Id == id && c.ParentId == parentId);

        if (child == null) return false;

        _db.Children.Remove(child);
        await _db.SaveChangesAsync();
        return true;
    }
}
