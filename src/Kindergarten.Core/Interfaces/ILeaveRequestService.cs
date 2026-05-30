using Kindergarten.Core.DTOs;
namespace Kindergarten.Core.Interfaces;
public interface ILeaveRequestService
{
    Task<LeaveRequestResponseDto>              CreateAsync(CreateLeaveRequestDto dto, string userId);
    Task<IEnumerable<LeaveRequestResponseDto>> GetByUserAsync(string userId);
    Task<IEnumerable<LeaveRequestResponseDto>> GetAllAsync();
    Task<LeaveRequestResponseDto?>             ReviewAsync(int id, ReviewLeaveRequestDto dto, string reviewerId);
    Task<double>                               GetMonthlyHoursAsync(string userId);
}
