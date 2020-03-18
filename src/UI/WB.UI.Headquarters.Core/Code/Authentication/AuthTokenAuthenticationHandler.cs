﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Core.GenericSubdomains.Portable;
using WB.UI.Shared.Web.Authentication;

namespace WB.UI.Headquarters.Code.Authentication
{
    public class AuthTokenAuthenticationHandler : AuthenticationHandler<AuthTokenAuthenticationSchemeOptions>
    {
        private readonly IUserRepository userRepository;
        private readonly IApiTokenProvider authTokenProvider;
        private bool isUserLocked;

        public AuthTokenAuthenticationHandler(IOptionsMonitor<AuthTokenAuthenticationSchemeOptions> options, 
            ILoggerFactory logger,
            UrlEncoder encoder, 
            ISystemClock clock,
            IUserRepository userRepository,
            IApiTokenProvider authTokenProvider) : base(options, logger, encoder, clock)
        {
            this.userRepository = userRepository;
            this.authTokenProvider = authTokenProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.NoResult();
            BasicCredentials creds;
            try
            {
                creds = Request.Headers.ParseBasicCredentials();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Information, e, "failed to authorize");
                return AuthenticateResult.NoResult();
            }

            var user = await userRepository.FindByNameAsync(creds.Username);
            if (user.IsArchivedOrLocked)
            {
                this.isUserLocked = true;
                return AuthenticateResult.Fail("User is locked");
            }

            var verificationResult = await authTokenProvider.ValidateTokenAsync(user.Id, creds.Password);
            if (verificationResult)
            {
                var claims = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.FormatGuid())
                });

                foreach (var userRole in user.Roles)
                {
                    claims.AddClaim(new Claim(ClaimTypes.Role, userRole.Name));
                }

                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claims), Scheme.Name));
            }

            return AuthenticateResult.Fail("Invalid auth token");
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            await base.HandleChallengeAsync(properties);
            if (this.isUserLocked)
            {
                await using StreamWriter bodyWriter = new StreamWriter(Response.Body);
                await bodyWriter.WriteAsync(JsonConvert.SerializeObject(new {Message = "User is locked"}));
            }
        }
    }
}
