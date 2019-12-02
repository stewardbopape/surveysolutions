using System;
using System.Data.Entity.Utilities;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Main.Core.Entities.SubEntities;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable;
using System.Threading;
using System.Web;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity.Providers;

namespace WB.Core.BoundedContexts.Headquarters.OwinSecurity
{
    public enum SignInStatus
    {
        Success,
        Failure,
        LockedOut
    }

    public interface ISignInManager
    {
        Task<SignInStatus> SignInAsync(string userName, string password, bool isPersistent = false);
    }

    public class HqSignInManager : ISignInManager
    {
        private readonly HqUserManager UserManager;
        private readonly IAuthenticationManager AuthenticationManager;
        private readonly IHashCompatibilityProvider hashCompatibilityProvider;
        private IApiTokenProvider ApiTokenProvider { get; set; }

        public HqSignInManager(HqUserManager userManager, 
            IAuthenticationManager authenticationManager,
            IHashCompatibilityProvider hashCompatibilityProvider,
            IApiTokenProvider tokenProvider = null)
        {
            this.ApiTokenProvider = tokenProvider ?? new ApiAuthTokenProvider(userManager);
            this.UserManager = userManager;
            this.AuthenticationManager = authenticationManager;
            this.hashCompatibilityProvider = hashCompatibilityProvider;
        }

        public async Task<SignInStatus> SignInAsync(string userName, string password, bool isPersistent = false)
        {
            var user = await this.UserManager.FindByNameAsync(userName);
            if (user == null || !await this.UserManager.CheckPasswordAsync(user, password))
                return SignInStatus.Failure;

            if (user.IsLockedByHeadquaters || user.IsLockedBySupervisor || user.IsArchived)
                return SignInStatus.LockedOut;

            await this.SignInAsync(user, isPersistent, false);

            return SignInStatus.Success;
        }
     
        public async Task SignInAsObserverAsync(string userName)
        {
            var userToObserve = await this.UserManager.FindByNameAsync(userName);
            userToObserve.Claims.Add(new HqUserClaim
            {
                UserId = userToObserve.Id,
                ClaimType = AuthorizedUser.ObserverClaimType,
                ClaimValue = this.AuthenticationManager.User.Identity.Name
            });
            userToObserve.Claims.Add(new HqUserClaim
            {
                UserId = userToObserve.Id,
                ClaimType = ClaimTypes.Role,
                ClaimValue = Enum.GetName(typeof(UserRoles), UserRoles.Observer)
            });

            await this.SignInAsync(userToObserve, true, true);
        }

        public async Task SignInBackFromObserverAsync()
        {
            var observerName = this.AuthenticationManager.User.FindFirst(AuthorizedUser.ObserverClaimType)?.Value;
            var observer = await this.UserManager.FindByNameAsync(observerName);

            this.AuthenticationManager.SignOut();

            await this.SignInAsync(observer, true, true);
        }

        public async Task<IdentityResult> SignInWithAuthTokenAsync(string authorizationHeader, bool treatPasswordAsPlain, params UserRoles[] allowedRoles)
        {
            BasicCredentials basicCredentials = this.ExtractFromAuthorizationHeader(ApiAuthenticationScheme.Basic, authorizationHeader)
                                              ?? this.ExtractFromAuthorizationHeader(ApiAuthenticationScheme.AuthToken, authorizationHeader);

            if (basicCredentials == null)
            {
                return IdentityResult.Failed();
            }

            var userInfo = await UserManager.FindByNameAsync(basicCredentials.Username);

            if (userInfo == null || userInfo.IsArchived)
            {
                return IdentityResult.Failed();
            }

            switch (basicCredentials.Scheme)
            {
                case ApiAuthenticationScheme.Basic:
                    if (treatPasswordAsPlain && !(await this.UserManager.CheckPasswordAsync(userInfo, basicCredentials.Password)))
                    {
                        return IdentityResult.Failed();
                    }
                    else if (!treatPasswordAsPlain && !this.CheckHashedPassword(userInfo, basicCredentials))
                    {
                        if (!IsInCompatibilityMode(userInfo))
                        {
                            return IdentityResult.Failed("UpgradeRequired");
                        }

                        return IdentityResult.Failed();
                    }
                    break;
                case ApiAuthenticationScheme.AuthToken:
                    if (!await ValidateApiAuthTokenAsync(userInfo.Id, basicCredentials.Password))
                    {
                        return IdentityResult.Failed();
                    }
                    break;
            }
            
            if (userInfo.IsLockedBySupervisor || userInfo.IsLockedByHeadquaters)
            {
                return IdentityResult.Failed("Locked");
            }
            
            if (!allowedRoles.Select(x => x.ToUserId()).Contains(userInfo.Roles.First().Id))
            {
                return IdentityResult.Failed("Role");
            }

            var identity = await this.UserManager.CreateIdentityAsync(userInfo, basicCredentials.Scheme.ToString());

            SetPrincipal(identity);

            return IdentityResult.Success;
        }

        public async Task<SignInStatus> SignInInterviewerAsync(string userName, string password, bool isPersistent = false)
        {
            var user = await this.UserManager.FindByNameAsync(userName);
            if (user == null || !await this.UserManager.CheckPasswordAsync(user, password))
                return SignInStatus.Failure;

            if (user.IsLockedByHeadquaters || user.IsLockedBySupervisor || user.IsArchived)
                return SignInStatus.LockedOut;
            
            var identity = await this.UserManager.CreateIdentityAsync(user, "Interviewer");
            SetPrincipal(identity);

            return SignInStatus.Success;
        }
        
        public async Task<SignInStatus> SignInSupervisorAsync(string userName, string password, bool isPersistent = false)
        {
            var user = await this.UserManager.FindByNameAsync(userName);
            if (user == null || !await this.UserManager.CheckPasswordAsync(user, password))
                return SignInStatus.Failure;

            if (user.IsLockedByHeadquaters || user.IsArchived)
                return SignInStatus.LockedOut;
            
            var identity = await this.UserManager.CreateIdentityAsync(user, "Supervisor");
            SetPrincipal(identity);

            return SignInStatus.Success;
        }

        private async Task SignInAsync(HqUser user, bool isPersistent, bool rememberBrowser)
        {
            var userIdentity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie).WithCurrentCulture();
            // Clear any partial cookies from external or two factor partial sign ins
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie, DefaultAuthenticationTypes.TwoFactorCookie);
            if (rememberBrowser)
            {
                var rememberBrowserIdentity = new ClaimsIdentity(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
                rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Convert.ToString(user.Id, CultureInfo.InvariantCulture)));

                AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = isPersistent }, userIdentity, rememberBrowserIdentity);
            }
            else
            {
                AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = isPersistent }, userIdentity);
            }
        }
        
        private void SetPrincipal(ClaimsIdentity identity)
        {
            var principal = new ClaimsPrincipal(identity);

            Thread.CurrentPrincipal = principal;
            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        public Task<string> GenerateApiAuthTokenAsync(Guid userId)
        {
            return this.ApiTokenProvider.GenerateTokenAsync(userId);
        }

        public Task<bool> ValidateApiAuthTokenAsync(Guid userId, string token)
        {
            return this.ApiTokenProvider.ValidateTokenAsync(userId, token);
        }

        private bool IsInCompatibilityMode(HqUser userInfo)
        {
            return hashCompatibilityProvider.IsInSha1CompatibilityMode() && userInfo.IsInRole(UserRoles.Interviewer);
        }

        private bool CheckHashedPassword(HqUser userInfo, BasicCredentials basicCredentials)
        {
            if (this.IsInCompatibilityMode(userInfo))
            {
                return userInfo.PasswordHashSha1 == basicCredentials.Password;
            }

            return userInfo.PasswordHash == basicCredentials.Password;
        }

        private BasicCredentials ExtractFromAuthorizationHeader(ApiAuthenticationScheme scheme, string authorizationHeader)
        {
            try
            {
                string schemeString = scheme.ToString();
                string header = authorizationHeader;

                if (header == null) return null;
                if (!header.StartsWith(schemeString, StringComparison.OrdinalIgnoreCase)) return null;

                string credentials = Encoding.ASCII.GetString(Convert.FromBase64String(header.Substring(schemeString.Length + 1)));
                int splitOn = credentials.IndexOf(':');

                return new BasicCredentials
                {
                    Username = credentials.Substring(0, splitOn),
                    Password = credentials.Substring(splitOn + 1),
                    Scheme = scheme
                };
            }
            catch
            {
            }

            return null;
        }

        internal class BasicCredentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public ApiAuthenticationScheme Scheme { get; set; }
        }
    }
}
