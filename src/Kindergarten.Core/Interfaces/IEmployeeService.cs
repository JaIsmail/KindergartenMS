using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeResponseDto>> GetAllAsync();
    Task<EmployeeResponseDto?>             GetByIdAsync(int id);
    Task<EmployeeResponseDto?>             GetByUserIdAsync(string userId);
    Task<EmployeeResponseDto>              CreateAsync(CreateEmployeeDto dto);
    Task<AttendanceResponseDto?>           CheckInAsync(string userId, double? lat = null, double? lng = null);
    Task<AttendanceResponseDto?>           CheckOutAsync(string userId);
    Task<IEnumerable<AttendanceResponseDto>> GetAttendanceAsync(string userId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<AttendanceResponseDto>> GetAllAttendanceAsync(DateTime? date = null);
    Task<IEnumerable<AttendanceResponseDto>> GetMyAttendanceAsync(string userId);
    Task EnsureDriverExistsAsync(string userId);
}
