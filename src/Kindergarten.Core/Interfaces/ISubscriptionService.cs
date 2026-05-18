using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionResponseDto>> GetAllAsync(string parentId);
    Task<SubscriptionResponseDto?>             GetByIdAsync(int id);
    Task<SubscriptionResponseDto>              CreateAsync(CreateSubscriptionDto dto, string parentId);
}
