using Contracts;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace BDayServer.ActionFilters
{
    public class ValidateEventExistsAttribute : IAsyncActionFilter
    {
        private readonly IRepositoryManager _repository;

        public ValidateEventExistsAttribute(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var trackChanges = context.HttpContext.Request.Method.Equals("PUT");
            var id = (Guid)context.ActionArguments["id"];
            var person = await _repository.Person.GetPersonAsync(id, trackChanges);

            if (person == null)
            {
                context.Result = new NotFoundResult();
            }
            else
            {
                context.HttpContext.Items.Add("event", person);
                await next();
            }
        }
    }
}
