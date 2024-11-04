using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BDayServer.ActionFilters;

public class ValidatePersonExistsAttribute : IAsyncActionFilter
{
    private readonly IRepositoryManager _repository;
    private readonly ILogger<ValidatePersonExistsAttribute> _logger;

    public ValidatePersonExistsAttribute(IRepositoryManager repository, ILogger<ValidatePersonExistsAttribute> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var trackChanges = context.HttpContext.Request.Method.Equals("PUT");

        Guid id;

        try
        {
            var idString = context.ActionArguments["id"];

            if (idString is null)
                throw new ArgumentNullException($"{nameof(context.ActionArguments)} id field");

            var result = Guid.TryParse(idString.ToString(), out id);

            if (result == false)
                throw new ArgumentException("Not valid Guid");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"{nameof(ArgumentNullException)}: {ex}");
            throw;
        }
        catch (FormatException ex)
        {
            _logger.LogError($"{nameof(FormatException)}: {ex}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(Exception)}: {ex}");
            throw;
        }

        var person = await _repository.Person.GetPersonAsync(id, trackChanges);

        if (person is null)
            context.Result = new NotFoundResult();
        else
        {
            context.HttpContext.Items.Add("person", person);
            await next();
        }
    }
}