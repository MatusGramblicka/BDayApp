using System.Text.Json;
using Contracts.DataTransferObjects.Event;
using Core.Mapping;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Repository.DatabaseAccessInterfaces;
using Repository.Services;

namespace BDayServer.Controllers;

[Route("api/events")]
[ApiController]
[Authorize]
public class EventController(
    IRepositoryManager repository,
    IGetUserProvider userData,
    UserManager<User> userManager)
    : ControllerBase
{
    [HttpGet(Name = "GetEvents")]
    public async Task<IActionResult> GetEvents([FromQuery] EventParameters eventParameters,
        CancellationToken cancellationToken)
    {
        var eventsFromDb = await repository.Event.GetAllEventsAsync(eventParameters, trackChanges: false,
            cancellationToken);

        Response.Headers["X-Pagination"] = JsonSerializer.Serialize(eventsFromDb.MetaData);

        return Ok(eventsFromDb.Select(e => e.ToDto()).ToList());
    }

    [HttpGet("{id:guid}", Name = "EventById")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var eventEntity = await repository.Event.GetEventAsync(id, trackChanges: false);

        return eventEntity is null ? NotFound() : Ok(eventEntity.ToDto());
    }

    [HttpPost(Name = "CreateEvent")]
    public async Task<IActionResult> CreateEvent([FromBody] EventForCreationDto eventParam)
    {
        var user = userData.UserName is null ? null : await userManager.FindByNameAsync(userData.UserName);
        if (user is null)
            return Unauthorized();

        var eventEntity = eventParam.ToEntity();
        eventEntity.UserId = user.Id;
        repository.Event.CreateEvent(eventEntity);
        await repository.SaveAsync();

        var eventToReturn = eventEntity.ToDto();

        return CreatedAtRoute("EventById", new { id = eventToReturn.Id }, eventToReturn);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventForUpdateDto eventDto)
    {
        var eventEntity = await repository.Event.GetEventAsync(id, trackChanges: true);
        if (eventEntity is null)
            return NotFound();

        eventDto.ApplyTo(eventEntity);
        await repository.SaveAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var eventEntity = await repository.Event.GetEventAsync(id, trackChanges: true);
        if (eventEntity is null)
            return NotFound();

        repository.Event.DeleteEvent(eventEntity);
        await repository.SaveAsync();

        return NoContent();
    }
}
