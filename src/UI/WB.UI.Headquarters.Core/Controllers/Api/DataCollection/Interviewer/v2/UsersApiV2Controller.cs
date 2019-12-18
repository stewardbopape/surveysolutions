﻿using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.SharedKernels.DataCollection.WebApi;

namespace WB.UI.Headquarters.API.DataCollection.Interviewer.v2
{
    [Route("api/interviewer/v2/users")]
    public class UsersApiV2Controller : UsersControllerBase
    {
        private readonly UserManager<HqUser> userManager;
        private readonly SignInManager<HqUser> signInManager;
        private readonly IApiTokenProvider apiAuthTokenProvider;

        public UsersApiV2Controller(
            IAuthorizedUser authorizedUser,
            UserManager<HqUser> userManager,
            SignInManager<HqUser> signInManager,
            IUserViewFactory userViewFactory,
            IApiTokenProvider apiAuthTokenProvider,
            IUserToDeviceService userToDeviceService) : base(
                authorizedUser: authorizedUser,
                userViewFactory: userViewFactory,
                userToDeviceService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.apiAuthTokenProvider = apiAuthTokenProvider;
        }

        [HttpGet]
        [Authorize(Roles = "Interviewer")]
        [Route("supervisor")]
        public Guid Supervisor()
        {
            var user = userViewFactory.GetUser(new UserViewInputModel(this.authorizedUser.Id));
            return user.Supervisor.Id;
        }

        [HttpGet]
        [Authorize(Roles = "Interviewer")]
        [Route("current")]
        public override ActionResult<InterviewerApiView> Current() => base.Current();

        [HttpGet]
        [Authorize(Roles = "Interviewer")]
        [Route("hasdevice")]
        public override ActionResult<bool> HasDevice() => base.HasDevice();

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<string>> Login([FromBody]LogonInfo userLogin)
        {
            var user = await this.userManager.FindByNameAsync(userLogin.Username);

            if (user == null || user.IsLockedByHeadquaters || user.IsLockedBySupervisor || user.IsArchived)
                return Unauthorized();
            var signInResult = await this.signInManager.CheckPasswordSignInAsync(user, userLogin.Password, false);
            if (signInResult.Succeeded)
            {
                var authToken = await this.apiAuthTokenProvider.GenerateTokenAsync(user.Id);
                return new JsonResult(authToken);
            }

            return Unauthorized();
        }
    }
}
