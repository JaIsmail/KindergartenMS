using System.Security.Claims;
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
public class ChildrenController : ControllerBase
{
    private readonly IChildService _childService;
    private readonly IAuditService _audit;
    public ChildrenController(IChildService childService, IAuditService audit) { _childService = childService; _audit = audit; }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    [RequirePermission("Children.View")]
    public async Task<IActionResult> GetAll()
    {
        var canViewAll = User.HasClaim("Permission", "Children.View") && User.HasClaim("Permission", "Users.View");
        var parentId = canViewAll ? null : GetUserId();
        var children = await _childService.GetAllAsync(parentId);
        return Ok(children);
    }

    [HttpGet("{id}")]
    [RequirePermission("Children.View")]
    public async Task<IActionResult> GetById(int id)
    {
        var child = await _childService.GetByIdAsync(id, GetUserId());
        if (child == null) return NotFound();
        return Ok(child);
    }

    [HttpPost]
    [RequirePermission("Children.Add")]
    public async Task<IActionResult> Create([FromBody] CreateChildDto dto)
    {
        var canAssign = User.HasClaim("Permission", "Children.Edit");
        var parentId  = (canAssign && !string.IsNullOrEmpty(dto.ParentId))
                        ? dto.ParentId
                        : GetUserId();
        var child = await _childService.CreateAsync(dto, parentId);
        return CreatedAtAction(nameof(GetById), new { id = child.Id }, child);
    }

    [HttpDelete("{id}")]
    [RequirePermission("Children.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _childService.DeleteAsync(id, GetUserId());
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPut("{id}")]
    [RequirePermission("Children.Edit")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateChildDto dto)
    {
        var child = await _childService.UpdateAsync(id, dto, GetUserId());
        if (child == null) return NotFound();
        return Ok(child);
    }

}
