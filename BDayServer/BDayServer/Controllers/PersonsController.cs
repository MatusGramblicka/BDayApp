using AutoMapper;
using BDayServer.ActionFilters;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BDayServer.Controllers
{
    [Route("api/persons")]
    [ApiController]
    [Authorize]
    public class PersonsController : ControllerBase
    {
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;

        public PersonsController(IRepositoryManager repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet(Name = "GetPersons")]
        public async Task<IActionResult> GetPersons([FromQuery] PersonParameters personParameters)
        {
            var personsFromDB = await _repository.Person.GetAllPersonsAsync(personParameters, trackChanges: false);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(personsFromDB.MetaData));

            var personsDto = _mapper.Map<IEnumerable<PersonDto>>(personsFromDB);

            return Ok(personsDto);
        }

        [HttpGet("{id}", Name = "PersonById")]
        public async Task<IActionResult> GetPerson(Guid id)
        {
            var person = await _repository.Person.GetPersonAsync(id, trackChanges: false);
            if (person == null)           
            {               
                return NotFound();
            }
            else
            {
                var personDto = _mapper.Map<PersonDto>(person);
                return Ok(personDto);
            }
        }

        [HttpPost(Name = "CreatePerson")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreatePerson([FromBody] PersonForCreationDto person)
        {
            var personEntity = _mapper.Map<Person>(person);

            _repository.Person.CreatePerson(personEntity);
            await _repository.SaveAsync();

            var personToReturn = _mapper.Map<PersonDto>(personEntity);

            return CreatedAtRoute("PersonById", new { id = personToReturn.Id }, personToReturn);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidatePersonExistsAttribute))]
        public async Task<IActionResult> UpdatePerson(Guid id, [FromBody] PersonForUpdateDto person)
        {
            var personEntity = HttpContext.Items["person"] as Person;

            _mapper.Map(person, personEntity);
            await _repository.SaveAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ServiceFilter(typeof(ValidatePersonExistsAttribute))]
        public async Task<IActionResult> DeletePerson(Guid id)
        {
            var person = HttpContext.Items["person"] as Person;

            _repository.Person.DeletePerson(person);
            await _repository.SaveAsync();

            return NoContent();
        }
    }
}
