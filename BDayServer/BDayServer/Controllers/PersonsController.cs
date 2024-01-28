using AutoMapper;
using BDayServer.ActionFilters;
using Contracts;
using Entities;
using Entities.DataTransferObjects;
using Entities.DataTransferObjects.Person;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private readonly UserManager<User> _userManager;

        private readonly string _userName;

        public PersonsController(IRepositoryManager repository, IMapper mapper, 
            IGetUserProvider userData, UserManager<User> userManager)
        {
            _repository = repository;
            _mapper = mapper;
            _userManager = userManager;

            _userName = userData.UserName;
        }

        [HttpGet(Name = "GetPersons")]
        public async Task<IActionResult> GetPersons([FromQuery] PersonParameters personParameters)
        {
            var personsFromDb = await _repository.Person.GetAllPersonsAsync(personParameters, trackChanges: false);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(personsFromDb.MetaData));

            var personsDto = _mapper.Map<IEnumerable<PersonDto>>(personsFromDb);

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

            var personDto = _mapper.Map<PersonDto>(person);
            return Ok(personDto);
        }

        [HttpPost(Name = "CreatePerson")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreatePerson([FromBody] PersonForCreationDto person)
        {
            if (_userName == null)
            {
                BadRequest("User is null");
            }

            var user = await _userManager.FindByNameAsync(_userName);

            if (user == null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    ErrorMessage = "Invalid Request"
                });
            }

            var personEntity = _mapper.Map<Person>(person);
            //personEntity.PersonCreator = _userName;
            personEntity.UserId = user.Id;

            _repository.Person.CreatePerson(personEntity);
            await _repository.SaveAsync();

            var personToReturn = _mapper.Map<PersonDto>(personEntity);

            return CreatedAtRoute("PersonById", new {id = personToReturn.Id}, personToReturn);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidatePersonExistsAttribute))]
        public async Task<IActionResult> UpdatePerson(Guid id, [FromBody] PersonForUpdateDto personDto)
        {
            var personEntity = HttpContext.Items["person"] as Person;

            //todo is personCreator overwritten?
            _mapper.Map(personDto, personEntity);
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
