using System.Text.Json;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/persons")]
[ApiController]
[Authorize]
public class PersonsController(IPersonManager personManager) : ControllerBase
{
    [HttpGet(Name = "GetPersons")]
    public async Task<IActionResult> GetPersons([FromQuery] PersonParameters personParameters,
        CancellationToken cancellationToken)
    {
        var persons = await personManager.GetPersonsAsync(personParameters, cancellationToken);

        Response.Headers["X-Pagination"] = JsonSerializer.Serialize(persons.MetaData);

        return Ok(persons);
    }

    [HttpGet("{id:guid}", Name = "PersonById")]
    public async Task<IActionResult> GetPerson(Guid id)
    {
        var personDto = await personManager.GetPersonAsync(id);

        return personDto is null ? NotFound() : Ok(personDto);
    }

    [HttpPost(Name = "CreatePerson")]
    public async Task<IActionResult> CreatePerson([FromBody] PersonForCreationDto personForCreationDto)
    {
        try
        {
            var personToReturn = await personManager.CreatePersonAsync(personForCreationDto);

            return CreatedAtRoute("PersonById", new { id = personToReturn.Id }, personToReturn);
        }
        catch (UserNotExistException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePerson(Guid id, [FromBody] PersonForUpdateDto personDto)
    {
        try
        {
            await personManager.UpdatePersonAsync(id, personDto);
        }
        catch (PersonNotExistException ex)
        {
            return NotFound(ex.Message);
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePerson(Guid id)
    {
        try
        {
            await personManager.DeletePersonAsync(id);
        }
        catch (PersonNotExistException ex)
        {
            return NotFound(ex.Message);
        }

        return NoContent();
    }
}
