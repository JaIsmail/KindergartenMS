using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface ITripService
{
    Task<TripResponseDto>              CreateAsync(CreateTripDto dto);
    Task<TripResponseDto?>             GetByIdAsync(int id);
    Task<IEnumerable<TripResponseDto>> GetByDriverAsync(string driverId);
    Task<TripResponseDto?>             StartTripAsync(int id);
    Task<TripResponseDto?>             EndTripAsync(int id);
    Task<bool>                         UpdateChildStatusAsync(UpdateChildStatusDto dto);
    Task<bool>                         SaveLocationAsync(UpdateLocationDto dto);
}
