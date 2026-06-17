using Kindergarten.Api.Authorization;
using Kindergarten.Core.Interfaces;
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
    private readonly IAuditService _audit;
    public PaymentsController(IPaymentService paymentService, IAuditService audit)
    {
        _paymentService = paymentService;
        _audit = audit;
    }

    [HttpGet("subscription/{subscriptionId}")]
    [RequirePermission("Payments.View")]
    public async Task<IActionResult> GetBySubscription(int subscriptionId)
    {
        var payments = await _paymentService.GetBySubscriptionAsync(subscriptionId);
        return Ok(payments);
    }

    [HttpPost]
    [RequirePermission("Payments.Add")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        var payment = await _paymentService.CreateAsync(dto);
        await _audit.LogAsync("Add", "Payment", payment.Id.ToString(), $"Amount: {dto.Amount} via {dto.Method}");
        return Ok(payment);
    }

    [HttpPut("{id}")]
    [RequirePermission("Payments.Edit")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentStatusDto dto)
    {
        var payment = await _paymentService.UpdateAsync(id, dto);
        if (payment == null) return NotFound();
        await _audit.LogAsync("Edit", "Payment", id.ToString(), $"Status changed to: {dto.Status}");
        return Ok(payment);
    }

    [HttpDelete("{id}")]
    [RequirePermission("Payments.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _paymentService.DeleteAsync(id);
        if (!result) return NotFound();
        await _audit.LogAsync("Delete", "Payment", id.ToString());
        return Ok(new { message = "Deleted" });
    }

    [HttpGet]
    [RequirePermission("Payments.View")]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _paymentService.GetAllAsync();
        return Ok(payments);
    }

    [HttpGet("overdue")]
    [RequirePermission("Payments.View")]
    public async Task<IActionResult> GetOverdue()
    {
        var overdue = await _paymentService.GetOverdueAsync();
        return Ok(overdue);
    }
}
