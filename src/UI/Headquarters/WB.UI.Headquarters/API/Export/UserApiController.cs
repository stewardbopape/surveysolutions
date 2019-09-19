﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.UI.Headquarters.API.Filters;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Headquarters.API.Export
{
    //[RoutePrefix("api/export/v1")]
    public class UserApiController : ApiController
    {
        private readonly IUserRepository userRepository;

        public UserApiController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [Route("api/export/v1/user/{id}")]
        [ServiceApiKeyAuthorization]
        [HttpGet]
        //[ApiNoCache]
        public HttpResponseMessage Get(string id)
        {
            var userId = Guid.Parse(id);
            var userModel = this.userRepository.Users
                .Where(user => user.Id == userId)
                .Select(user => new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    UserRole = user.Roles.Single()
                });

            return Request.CreateResponse(HttpStatusCode.OK, userModel);
        }
    }
}
