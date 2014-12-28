﻿using System;
using System.Web.Mvc;
using Main.Core.Entities.SubEntities;

using WB.Core.GenericSubdomains.Logging;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.GenericSubdomains.Utils.Implementation.Crypto;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Commands.User;
using WB.Core.SharedKernels.SurveyManagement.Web.Controllers;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Membership;

namespace WB.UI.Headquarters.Controllers
{
    public class InstallController : BaseController
    {
        private readonly IPasswordHasher passwordHasher;

        public InstallController(ICommandService commandService, IGlobalInfoProvider globalInfo, ILogger logger, IPasswordHasher passwordHasher)
            : base(commandService, globalInfo, logger)
        {
            this.passwordHasher = passwordHasher;
        }

        public ActionResult Finish()
        {
            return View(new UserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Finish(UserModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    this.CommandService.Execute(new CreateUserCommand(publicKey: Guid.NewGuid(), userName: model.UserName,
                    password: passwordHasher.Hash(model.Password), email: model.Email, isLockedBySupervisor: false,
                    isLockedByHQ: false, roles: new[] { UserRoles.Headquarter }, supervsor: null));
                    return this.RedirectToAction("LogOn", "Account");
                }
                catch (Exception ex)
                {
                    this.Logger.Fatal("Error when creating headquarters user", ex);
                }
            }

            return View(model);
        }
    }
}