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
    private readonly ITripService            _tripService;
    private readonly IHubContext<TripHub>    _hub;
    private readonly INotificationService   _notify;

    public TripsController(
        ITripService          tripService,
        IHubContext<TripHub>  hub,
        INotificationService  notify)
    {
        _tripService = tripService;
        _hub         = hub;
        _notify      = notify;
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

        // 🔔 Notify all parents
        await _notify.SendToAllParentsAsync(
            titleAr: "بدأت الرحلة",
            titleEn: "Trip Started",
            bodyAr:  "السائق في الطريق إليكم",
            bodyEn:  "The driver is on the way",
            data: new Dictionary<string, string> {
                { "type", "trip_started" },
                { "tripId", id.ToString() }
            }
        );

        return Ok(trip);
    }

    [HttpPut("{id}/end")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> EndTrip(int id)
    {
        var trip = await _tripService.EndTripAsync(id);
        if (trip == null) return NotFound();

        await _hub.Clients.All.SendAsync("TripStatusUpdated", id, "Completed");

        // 🔔 Notify all parents
        await _notify.SendToAllParentsAsync(
            titleAr: "انتهت الرحلة",
            titleEn: "Trip Completed",
            bodyAr:  "تمت الرحلة بنجاح",
            bodyEn:  "The trip has been completed successfully",
            data: new Dictionary<string, string> {
                { "type", "trip_completed" },
                { "tripId", id.ToString() }
            }
        );

        return Ok(trip);
    }

    [HttpPost("child-status")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> UpdateChildStatus([FromBody] UpdateChildStatusDto dto)
    {
        var result = await _tripService.UpdateChildStatusAsync(dto);
        if (!result) return NotFound();

        await _hub.Clients.All.SendAsync("ChildStatusUpdated", dto.TripId, dto.ChildId, dto.Status);

        // 🔔 Notify specific parent
        var parentId = await _tripService.GetChildParentIdAsync(dto.ChildId);
        if (!string.IsNullOrEmpty(parentId))
        {
            if (dto.Status == "PickedUp")
            {
                await _notify.SendToParentAsync(
                    parentId,
                    titleAr: "تم استلام طفلك",
                    titleEn: "Child Picked Up",
                    bodyAr:  "تم استلام طفلك بأمان",
                    bodyEn:  "Your child has been picked up safely",
                    data: new Dictionary<string, string> {
                        { "type", "child_picked_up" },
                        { "childId", dto.ChildId.ToString() },
                        { "tripId", dto.TripId.ToString() }
                    }
                );
            }
            else if (dto.Status == "DroppedOff")
            {
                await _notify.SendToParentAsync(
                    parentId,
                    titleAr: "تم توصيل طفلك",
                    titleEn: "Child Dropped Off",
                    bodyAr:  "وصل طفلك إلى المنزل بسلامة",
                    bodyEn:  "Your child has arrived home safely",
                    data: new Dictionary<string, string> {
                        { "type", "child_dropped_off" },
                        { "childId", dto.ChildId.ToString() },
                        { "tripId", dto.TripId.ToString() }
                    }
                );
            }
        }

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
