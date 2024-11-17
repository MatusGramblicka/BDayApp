using AutoMapper;
using BDayServer.ActionFilters;
using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.Event;
using Entities.Models;
using Entities.RequestFeatures;
using Interfaces;
using Interfaces.DatabaseAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BDayServer.Controllers;

[Route("api/events")]
[ApiController]
[Authorize]
public class EventController(
    IRepositoryManager repository,
    IMapper mapper,
    IGetUserProvider userData,
    UserManager<User> userManager)
    : Controller
{
    private readonly string _userName = userData.UserName;

    [HttpGet(Name = "GetEvents")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public IActionResult GetEvents([FromQuery] EventParameters eventParameters)
    {
        var eventsFromDb = repository.Event.GetAllEventsAsync(eventParameters, trackChanges: false);

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(eventsFromDb.MetaData));

        var eventsDto = mapper.Map<IEnumerable<EventDto>>(eventsFromDb);

        return Ok(eventsDto);
    }

    [HttpGet("{id}", Name = "EventById")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var eventVar = await repository.Event.GetEventAsync(id, trackChanges: false);
        if (eventVar is null)
        {
            return NotFound();
        }

        var eventDto = mapper.Map<EventDto>(eventVar);
        return Ok(eventDto);
    }

    [HttpPost(Name = "CreateEvent")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> CreateEvent([FromBody] EventForCreationDto eventParam)
    {
        if (_userName is null)
        {
            BadRequest("User is null");
        }

        var user = await userManager.FindByNameAsync(_userName);

        if (user is null)
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });
        }

        var eventEntity = mapper.Map<Event>(eventParam);
        eventEntity.UserId = user.Id;

        repository.Event.CreateEvent(eventEntity);
        await repository.SaveAsync();

        var eventToReturn = mapper.Map<EventDto>(eventEntity);

        return CreatedAtRoute("EventById", new { id = eventToReturn.Id }, eventToReturn);
    }

    [HttpPut("{id}")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventForUpdateDto eventDto)
    {
        var eventEntity = HttpContext.Items["event"] as Event;

        mapper.Map(eventDto, eventEntity);
        await repository.SaveAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var eventVar = HttpContext.Items["event"] as Event;

        repository.Event.DeleteEvent(eventVar);
        await repository.SaveAsync();

        return NoContent();
    }
}