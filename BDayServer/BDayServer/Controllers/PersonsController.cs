using BDayServer.ActionFilters;
using Contracts.Exceptions;
using Contracts.Managers;
using Entities.DataTransferObjects.Person;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BDayServer.Controllers;

[Route("api/persons")]
[ApiController]
[Authorize]
public class PersonsController(IPersonManager personManager) : ControllerBase
{
    [HttpGet(Name = "GetPersons")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public IActionResult GetPersons([FromQuery] PersonParameters personParameters)
    {
        var personsDto = personManager.GetPersons(personParameters);

        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(personsDto.MetaData);

        return Ok(personsDto);
    }

    [HttpGet("{id}", Name = "PersonById")]
    public async Task<IActionResult> GetPerson(Guid id)
    {
        var personDto = await personManager.GetPersonAsync(id);

        return personDto is null ? NotFound() : Ok(personDto);
    }

    [HttpPost(Name = "CreatePerson")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> CreatePerson([FromBody] PersonForCreationDto personForCreationDto)
    {
        if (personForCreationDto is null)
            return BadRequest("Object is null");

        PersonDto personToReturn;

        try
        {
            personToReturn = await personManager.CreatePersonAsync(personForCreationDto);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UserNotExistException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return CreatedAtRoute("PersonById", new {id = personToReturn.Id}, personToReturn);
    }

    [HttpPut("{id}")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> UpdatePerson(Guid id, [FromBody] PersonForUpdateDto personDto)
    {
        try
        {
            await personManager.UpdatePersonAsync(id, personDto);
        }
        catch (PersonNotExistException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePerson(Guid id)
    {
        try
        {
            await personManager.DeletePersonAsync(id);
        }
        catch (PersonNotExistException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }
}