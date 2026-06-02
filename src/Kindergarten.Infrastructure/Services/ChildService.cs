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

    public async Task<IEnumerable<ChildResponseDto>> GetAllAsync(string? parentId)
    {
        var query = _db.Children.Include(c => c.Parent).AsQueryable();
        if(!string.IsNullOrEmpty(parentId))
            query = query.Where(c => c.ParentId == parentId);
        return await query
            .Select(c => new ChildResponseDto
            {
                Id           = c.Id,
                Name         = c.Name,
                NationalId   = c.NationalId,
                BirthDate    = c.BirthDate,
                Class        = c.Class,
                AgeGroup     = c.AgeGroup,
                MotherPhone  = c.MotherPhone,
                Neighborhood = c.Neighborhood,
                HealthNotes  = c.HealthNotes,
                IsActive     = c.IsActive,
                ParentId     = c.ParentId,
                ParentName   = c.Parent.FullName
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
            Id           = c.Id,
            Name         = c.Name,
            NationalId   = c.NationalId,
            BirthDate    = c.BirthDate,
            Class        = c.Class,
            AgeGroup     = c.AgeGroup,
            MotherPhone  = c.MotherPhone,
            Neighborhood = c.Neighborhood,
            HealthNotes  = c.HealthNotes,
            IsActive     = c.IsActive,
            ParentId     = c.ParentId,
            ParentName   = c.Parent.FullName
        };
    }

    public async Task<ChildResponseDto> CreateAsync(CreateChildDto dto, string parentId)
    {
        var child = new Child
        {
            Name         = dto.Name,
            NationalId   = dto.NationalId,
            BirthDate    = dto.BirthDate,
            Class        = dto.Class,
            AgeGroup     = dto.AgeGroup,
            MotherPhone  = dto.MotherPhone,
            Neighborhood = dto.Neighborhood,
            HealthNotes  = dto.HealthNotes,
            IsActive     = dto.IsActive,
            ParentId     = parentId,
            TenantId     = _tenantService.GetTenantId()
        };

        _db.Children.Add(child);
        await _db.SaveChangesAsync();

        var parent = await _db.Users.FindAsync(parentId);

        return new ChildResponseDto
        {
            Id           = child.Id,
            Name         = child.Name,
            NationalId   = child.NationalId,
            BirthDate    = child.BirthDate,
            Class        = child.Class,
            AgeGroup     = child.AgeGroup,
            MotherPhone  = child.MotherPhone,
            Neighborhood = child.Neighborhood,
            HealthNotes  = child.HealthNotes,
            IsActive     = child.IsActive,
            ParentId     = child.ParentId,
            ParentName   = parent?.FullName ?? string.Empty
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

    public async Task<ChildResponseDto?> UpdateAsync(int id, CreateChildDto dto, string parentId)
    {
        var canViewAll = true; // called from admin context
        var child = await _db.Children
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (child == null) return null;

        child.Name         = dto.Name;
        child.NationalId   = dto.NationalId;
        child.BirthDate    = dto.BirthDate;
        child.Class        = dto.Class;
        child.AgeGroup     = dto.AgeGroup;
        child.MotherPhone  = dto.MotherPhone;
        child.Neighborhood = dto.Neighborhood;
        child.HealthNotes  = dto.HealthNotes;
        child.IsActive     = dto.IsActive;
        if (!string.IsNullOrEmpty(dto.ParentId))
            child.ParentId = dto.ParentId;

        await _db.SaveChangesAsync();

        return new ChildResponseDto
        {
            Id           = child.Id,
            Name         = child.Name,
            NationalId   = child.NationalId,
            BirthDate    = child.BirthDate,
            Class        = child.Class,
            AgeGroup     = child.AgeGroup,
            MotherPhone  = child.MotherPhone,
            Neighborhood = child.Neighborhood,
            HealthNotes  = child.HealthNotes,
            IsActive     = child.IsActive,
            ParentId     = child.ParentId,
            ParentName   = child.Parent?.FullName ?? string.Empty
        };
    }

}
