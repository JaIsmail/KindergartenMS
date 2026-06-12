using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class TripService : ITripService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    public TripService(ApplicationDbContext db, ITenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<TripResponseDto> CreateAsync(CreateTripDto dto)
    {
        var trip = new Trip
        {
            DriverId  = dto.DriverId,
            Direction = dto.Direction,
            Date      = dto.Date == default ? DateTime.UtcNow : dto.Date,
            Status    = "Created",
            TenantId  = _tenantService.GetTenantId()
        };
        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        foreach (var childId in dto.ChildIds)
        {
            _db.TripChildren.Add(new TripChild
            {
                TripId        = trip.Id,
                ChildId       = childId,
                PickupStatus  = "Pending",
                DropoffStatus = "Pending",
                TenantId      = _tenantService.GetTenantId()
            });
        }
        await _db.SaveChangesAsync();
        return await GetByIdAsync(trip.Id) ?? new TripResponseDto();
    }

    public async Task<TripResponseDto?> GetByIdAsync(int id)
    {
        var trip = await _db.Trips
            .IgnoreQueryFilters().Include(t => t.Driver)
            .Include(t => t.TripChildren).ThenInclude(tc => tc.Child)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return null;
        return MapToDto(trip);
    }

    public async Task<IEnumerable<TripResponseDto>> GetByDriverAsync(string driverId)
    {
        var trips = await _db.Trips
            .IgnoreQueryFilters().Include(t => t.Driver)
            .Include(t => t.TripChildren).ThenInclude(tc => tc.Child)
            .Where(t => t.DriverId == driverId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return trips.Select(MapToDto);
    }

    public async Task<TripResponseDto?> StartTripAsync(int id)
    {
        var trip = await _db.Trips.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (trip == null) return null;
        trip.Status    = "InProgress";
        trip.StartTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<TripResponseDto?> EndTripAsync(int id)
    {
        var trip = await _db.Trips.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (trip == null) return null;
        trip.Status  = "Completed";
        trip.EndTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> UpdateChildStatusAsync(UpdateChildStatusDto dto)
    {
        var tc = await _db.TripChildren
            .FirstOrDefaultAsync(x => x.TripId == dto.TripId && x.ChildId == dto.ChildId);
        if (tc == null) return false;

        if (dto.Type == "Pickup") { tc.PickupStatus = dto.Status; tc.PickupTime = DateTime.UtcNow; }
        else                      { tc.DropoffStatus = dto.Status; tc.DropoffTime = DateTime.UtcNow; }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveLocationAsync(UpdateLocationDto dto)
    {
        _db.TripLocations.Add(new TripLocation
        {
            TripId    = dto.TripId,
            Latitude  = dto.Latitude,
            Longitude = dto.Longitude,
            Timestamp = DateTime.UtcNow,
            TenantId  = _tenantService.GetTenantId()
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TripResponseDto>> GetAllTripsAsync()
    {
        var trips = await _db.Trips
            .IgnoreQueryFilters().Include(t => t.Driver)
            .Include(t => t.TripChildren).ThenInclude(tc => tc.Child)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return trips.Select(MapToDto);
    }

    public async Task<string?> GetChildParentIdAsync(int childId)
    {
        var child = await _db.Children.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == childId);
        return child?.ParentId;
    }

    private static TripResponseDto MapToDto(Trip t) => new()
    {
        Id         = t.Id,
        DriverId   = t.DriverId,
        DriverName = t.Driver?.FullName ?? string.Empty,
        Date       = t.Date,
        Direction  = t.Direction,
        Status     = t.Status,
        StartTime  = t.StartTime,
        EndTime    = t.EndTime,
        Children   = t.TripChildren.Select(tc => new TripChildStatusDto
        {
            ChildId       = tc.ChildId,
            ChildName     = tc.Child?.Name ?? string.Empty,
            PickupStatus  = tc.PickupStatus,
            DropoffStatus = tc.DropoffStatus,
            PickupTime    = tc.PickupTime,
            DropoffTime   = tc.DropoffTime
        }).ToList()
    };
}
