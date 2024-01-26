using AutoMapper;
using BDayServer.ActionFilters;
using Contracts;
using Entities;
using Entities.DataTransferObjects.Event;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BDayServer.Controllers
{
    [Route("api/events")]
    [ApiController]
    [Authorize]
    public class EventController : Controller
    {
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;

        private readonly string _userId;

        public EventController(IRepositoryManager repository, IMapper mapper, IGetUserProvider userData)
        {
            _repository = repository;
            _mapper = mapper;
            _userId = userData.UserName;
        }

        [HttpGet(Name = "GetEvents")]
        public async Task<IActionResult> GetEvents([FromQuery] EventParameters eventParameters)
        {
            var eventsFromDb = await _repository.Event.GetAllEventsAsync(eventParameters, trackChanges: false);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(eventsFromDb.MetaData));

            var eventsDto = _mapper.Map<IEnumerable<EventDto>>(eventsFromDb);

            return Ok(eventsDto);
        }

        [HttpGet("{id}", Name = "EventById")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var eventVar = await _repository.Event.GetEventAsync(id, trackChanges: false);
            if (eventVar == null)
            {
                return NotFound();
            }

            var eventDto = _mapper.Map<EventDto>(eventVar);
            return Ok(eventDto);
        }

        [HttpPost(Name = "CreateEvent")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateEvent([FromBody] EventForCreationDto eventParam)
        {
            if (_userId == null)
            {
                BadRequest("User is null");
            }

            var eventEntity = _mapper.Map<Event>(eventParam);
            eventEntity.EventCreator = _userId;

            _repository.Event.CreateEvent(eventEntity);
            await _repository.SaveAsync();

            var eventToReturn = _mapper.Map<EventDto>(eventEntity);

            return CreatedAtRoute("EventById", new { id = eventToReturn.Id }, eventToReturn);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidateEventExistsAttribute))]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventForUpdateDto eventDto)
        {
            var eventEntity = HttpContext.Items["event"] as Event;

            _mapper.Map(eventDto, eventEntity);
            await _repository.SaveAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ServiceFilter(typeof(ValidateEventExistsAttribute))]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var eventVar = HttpContext.Items["event"] as Event;

            _repository.Event.DeleteEvent(eventVar);
            await _repository.SaveAsync();

            return NoContent();
        }
    }
}
