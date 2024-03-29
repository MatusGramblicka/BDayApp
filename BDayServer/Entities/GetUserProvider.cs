﻿using Microsoft.AspNetCore.Http;

namespace Entities
{
    public class GetUserProvider : IGetUserProvider
    {
        public string UserName { get; }            

        public GetUserProvider(IHttpContextAccessor accessor)
        {
            if (accessor.HttpContext?.User.Identity != null)
            {
                UserName = accessor.HttpContext?.User.Identity.Name;
            }
        }
    }
}
