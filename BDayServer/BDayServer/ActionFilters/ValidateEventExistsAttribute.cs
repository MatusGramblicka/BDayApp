using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BDayServer.ActionFilters;

public class ValidateEventExistsAttribute : IAsyncActionFilter
{
    private readonly IRepositoryManager _repository;
    private readonly ILogger<ValidateEventExistsAttribute> _logger;

    public ValidateEventExistsAttribute(IRepositoryManager repository, ILogger<ValidateEventExistsAttribute> logger)
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

        var eventData = await _repository.Event.GetEventAsync(id, trackChanges);

        if (eventData is null)
            context.Result = new NotFoundResult();
        else
        {
            context.HttpContext.Items.Add("event", eventData);
            await next();
        }
    }
}