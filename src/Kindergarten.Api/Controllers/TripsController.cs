using System.Security.Claims;
using Kindergarten.Api.Hubs;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly ITripService       _tripService;
    private readonly IHubContext<TripHub> _hub;

    public TripsController(ITripService tripService, IHubContext<TripHub> hub)
    {
        _tripService = tripService;
        _hub         = hub;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTripDto dto)
    {
        var trip = await _tripService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null) return NotFound();
        return Ok(trip);
    }

    [HttpGet("driver")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> GetMyTrips()
    {
        var trips = await _tripService.GetByDriverAsync(GetUserId());
        return Ok(trips);
    }

    [HttpPut("{id}/start")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> StartTrip(int id)
    {
        var trip = await _tripService.StartTripAsync(id);
        if (trip == null) return NotFound();
        await _hub.Clients.All.SendAsync("TripStatusUpdated", id, "InProgress");
        return Ok(trip);
    }

    [HttpPut("{id}/end")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> EndTrip(int id)
    {
        var trip = await _tripService.EndTripAsync(id);
        if (trip == null) return NotFound();
        await _hub.Clients.All.SendAsync("TripStatusUpdated", id, "Completed");
        return Ok(trip);
    }

    [HttpPost("child-status")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> UpdateChildStatus([FromBody] UpdateChildStatusDto dto)
    {
        var result = await _tripService.UpdateChildStatusAsync(dto);
        if (!result) return NotFound();
        await _hub.Clients.All.SendAsync("ChildStatusUpdated", dto.TripId, dto.ChildId, dto.Status);
        return Ok(new { message = "Child status updated" });
    }

    [HttpPost("location")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto dto)
    {
        var result = await _tripService.SaveLocationAsync(dto);
        if (!result) return BadRequest();
        await _hub.Clients.All.SendAsync("LocationUpdated", dto.TripId, dto.Latitude, dto.Longitude);
        return Ok(new { message = "Location updated" });
    }
}
