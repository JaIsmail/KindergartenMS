using Kindergarten.Api.Authorization;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    public PaymentsController(IPaymentService paymentService) =>
        _paymentService = paymentService;

    [HttpGet("subscription/{subscriptionId}")]
    [RequirePermission("ViewFinancials")]
    public async Task<IActionResult> GetBySubscription(int subscriptionId)
    {
        var payments = await _paymentService.GetBySubscriptionAsync(subscriptionId);
        return Ok(payments);
    }

    [HttpPost]
    [RequirePermission("ManagePayments")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        var payment = await _paymentService.CreateAsync(dto);
        return Ok(payment);
    }
}
