using Kindergarten.Core.DTOs;
namespace Kindergarten.Core.Interfaces;
public interface IChildService
{
    Task<IEnumerable<ChildResponseDto>> GetAllAsync(string? parentId);
    Task<ChildResponseDto?>             GetByIdAsync(int id, string parentId);
    Task<ChildResponseDto>              CreateAsync(CreateChildDto dto, string parentId);
    Task<ChildResponseDto?>             UpdateAsync(int id, CreateChildDto dto, string parentId);
    Task<bool>                          DeleteAsync(int id, string parentId);
}
