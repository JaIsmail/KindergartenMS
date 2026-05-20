using System.Security.Claims;
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
    public ChildrenController(IChildService childService) => _childService = childService;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var children = await _childService.GetAllAsync(GetUserId());
        return Ok(children);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var child = await _childService.GetByIdAsync(id, GetUserId());
        if (child == null) return NotFound();
        return Ok(child);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChildDto dto)
    {
        // Admin can specify parentId, others use their own ID
        var role     = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var parentId = (role == "Admin" && !string.IsNullOrEmpty(dto.ParentId))
                       ? dto.ParentId
                       : GetUserId();
        var child = await _childService.CreateAsync(dto, parentId);
        return CreatedAtAction(nameof(GetById), new { id = child.Id }, child);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _childService.DeleteAsync(id, GetUserId());
        if (!result) return NotFound();
        return NoContent();
    }
}
