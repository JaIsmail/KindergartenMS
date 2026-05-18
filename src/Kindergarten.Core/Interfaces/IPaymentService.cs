using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface IPaymentService
{
    Task<IEnumerable<PaymentResponseDto>> GetBySubscriptionAsync(int subscriptionId);
    Task<PaymentResponseDto>              CreateAsync(CreatePaymentDto dto);
}
