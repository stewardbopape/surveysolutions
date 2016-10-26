using System;
using Microsoft.AspNet.Identity.EntityFramework;
using WB.Core.BoundedContexts.Headquarters.Views.User;

namespace WB.UI.Headquarters.Identity
{
    internal class AppUserStore : UserStore<ApplicationUser, AppRole, Guid, AppUserLogin, AppUserRole, AppUserClaim>, IAppUserStore
    {
        public AppUserStore() : base(new HQIdentityDbContext())
        {

        }

        public AppUserStore(HQIdentityDbContext context) : base(context)
        {

        }
    }
}