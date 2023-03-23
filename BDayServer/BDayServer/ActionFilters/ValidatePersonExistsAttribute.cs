using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace BDayServer.ActionFilters
{
    public class ValidatePersonExistsAttribute : IAsyncActionFilter
    {
        private readonly IRepositoryManager _repository;

        public ValidatePersonExistsAttribute(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var trackChanges = context.HttpContext.Request.Method.Equals("PUT");
            var id = (Guid) context.ActionArguments["id"];
            var person = await _repository.Person.GetPersonAsync(id, trackChanges);

            if (person == null)
            {
                context.Result = new NotFoundResult();
            }
            else
            {
                context.HttpContext.Items.Add("person", person);
                await next();
            }
        }
    }
}
