﻿using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Headquarters.Controllers
{
    public class InstallController : BaseController
    {
        private readonly ISupportedVersionProvider supportedVersionProvider;
        private readonly HqSignInManager signInManager;
        public readonly HqUserManager userManager;

        public InstallController(ICommandService commandService,
                                 ISupportedVersionProvider supportedVersionProvider,
                                 ILogger logger,
                                 HqSignInManager identityManager,
                                 HqUserManager userManager)
            : base(commandService, logger)
        {
            this.userManager = userManager;
            this.supportedVersionProvider = supportedVersionProvider;
            this.signInManager = identityManager;
        }

        public ActionResult Finish()
        {
            return View(new UserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PreventDoubleSubmit]
        public async Task<ActionResult> Finish(UserModel model)
        {
            if (ModelState.IsValid)
            {
                var creationResult = await this.userManager.CreateUserAsync(new HqUser
                {
                    Id = Guid.Parse(@"00000000000000000000000000000001"),
                    FullName = @"Administrator",
                    UserName = model.UserName,
                    Email = model.Email
                }, model.Password, UserRoles.Administrator);

                if (creationResult.Succeeded)
                {
                    await this.signInManager.SignInAsync(model.UserName, model.Password, isPersistent: false);

                    this.supportedVersionProvider.RememberMinSupportedVersion();

                    return this.RedirectToAction("Index", "Headquarters");
                }
                AddErrors(creationResult);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}