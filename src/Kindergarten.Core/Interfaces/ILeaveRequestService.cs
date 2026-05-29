using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequestResponseDto>              CreateAsync(CreateLeaveRequestDto dto, int employeeId);
    Task<IEnumerable<LeaveRequestResponseDto>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequestResponseDto>> GetAllAsync();
    Task<LeaveRequestResponseDto?>             ReviewAsync(int id, ReviewLeaveRequestDto dto, string adminId);
    Task<double>                               GetMonthlyHoursAsync(string userId);
}
