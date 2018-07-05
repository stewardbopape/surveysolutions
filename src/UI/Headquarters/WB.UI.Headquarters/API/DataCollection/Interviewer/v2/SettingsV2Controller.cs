﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.DataExport.Security;
using WB.Core.Infrastructure.PlainStorage;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Models.CompanyLogo;

namespace WB.UI.Headquarters.API.DataCollection.Interviewer.v2
{
    [ApiBasicAuth(new[] { UserRoles.Interviewer })]
    public class SettingsV2Controller : SettingsControllerBase
    {
        private readonly IPlainKeyValueStorage<InterviewerSettings> interviewerSettingsStorage;

        public SettingsV2Controller(IPlainKeyValueStorage<CompanyLogo> appSettingsStorage,
            IPlainKeyValueStorage<InterviewerSettings> interviewerSettingsStorage) : base(appSettingsStorage)
        {
            this.interviewerSettingsStorage = interviewerSettingsStorage;
        }


        [HttpGet]
        public override HttpResponseMessage CompanyLogo() => base.CompanyLogo();

        [HttpGet]
        public override bool AutoUpdateEnabled() =>
            this.interviewerSettingsStorage.GetById(AppSetting.InterviewerSettings)?.AutoUpdateEnabled ?? true;

    }
}
