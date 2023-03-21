using Microsoft.AspNetCore.Http;

namespace Entities
{
    public class GetUserProvider : IGetUserProvider
    {
        public string UserId { get; }            

        public GetUserProvider(IHttpContextAccessor accessor)
        {           
            UserId = accessor.HttpContext?.User.Identity.Name;       
        }
    }
}
