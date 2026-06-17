using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface IPaymentService
{
    Task<IEnumerable<PaymentResponseDto>> GetBySubscriptionAsync(int subscriptionId);
    Task<PaymentResponseDto>              CreateAsync(CreatePaymentDto dto);
    Task<IEnumerable<PaymentResponseDto>> GetAllAsync();
    Task<IEnumerable<OverdueSubscriptionDto>> GetOverdueAsync();
    Task<PaymentResponseDto?> UpdateAsync(int id, UpdatePaymentStatusDto dto);
    Task<bool> DeleteAsync(int id);
}
