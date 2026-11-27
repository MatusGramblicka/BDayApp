using System.Text.Json;
using AutoMapper;
using Contracts.DataTransferObjects.Event;
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
    IMapper mapper,
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

        return Ok(mapper.Map<IEnumerable<EventDto>>(eventsFromDb));
    }

    [HttpGet("{id:guid}", Name = "EventById")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var eventEntity = await repository.Event.GetEventAsync(id, trackChanges: false);

        return eventEntity is null ? NotFound() : Ok(mapper.Map<EventDto>(eventEntity));
    }

    [HttpPost(Name = "CreateEvent")]
    public async Task<IActionResult> CreateEvent([FromBody] EventForCreationDto eventParam)
    {
        var user = userData.UserName is null ? null : await userManager.FindByNameAsync(userData.UserName);
        if (user is null)
            return Unauthorized();

        var eventEntity = mapper.Map<Event>(eventParam);
        eventEntity.UserId = user.Id;
        repository.Event.CreateEvent(eventEntity);
        await repository.SaveAsync();

        var eventToReturn = mapper.Map<EventDto>(eventEntity);

        return CreatedAtRoute("EventById", new { id = eventToReturn.Id }, eventToReturn);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventForUpdateDto eventDto)
    {
        var eventEntity = await repository.Event.GetEventAsync(id, trackChanges: true);
        if (eventEntity is null)
            return NotFound();

        mapper.Map(eventDto, eventEntity);
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
