﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Invitations;
using WB.Core.BoundedContexts.Headquarters.ValueObjects;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.WebInterview;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Controllers;
using WB.UI.Headquarters.Resources;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Headquarters.API.WebInterview
{
    [RoutePrefix("api/v1/webInterviewSettings")]
    [Authorize(Roles = "Administrator, Headquarter")]
    [ApiNoCache]
    [CamelCase]
    public class WebInterviewSettingsApiController : BaseApiController
    {
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IWebInterviewConfigProvider webInterviewConfigProvider;
        private readonly IAssignmentsService assignmentsService;
        private readonly IInvitationService invitationService;
        private readonly IPlainKeyValueStorage<EmailProviderSettings> emailProviderSettingsStorage;
        private readonly IArchiveUtils archiveUtils;
        

        public WebInterviewSettingsApiController(
            ICommandService commandService, 
            ILogger logger, 
            IWebInterviewConfigProvider webInterviewConfigProvider,
            IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory, 
            IAssignmentsService assignmentsService,
            IInvitationService invitationService, 
            IPlainKeyValueStorage<EmailProviderSettings> emailProviderSettingsStorage,
            IArchiveUtils archiveUtils) : base(commandService, logger)
        {
            this.webInterviewConfigProvider = webInterviewConfigProvider;
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.assignmentsService = assignmentsService;
            this.invitationService = invitationService;
            this.emailProviderSettingsStorage = emailProviderSettingsStorage;
            this.archiveUtils = archiveUtils;
        }

        /*[Route(@"{id}/fetchEmailTemplates")]
        [HttpGet]
        public IHttpActionResult GetEmailTemplates(string id)
        {
            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
            {
                NotFound();
            }

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
            {
                NotFound();
            }

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            var emailTemplates = config.EmailTemplates.ToDictionary(t => t.Key, t =>
                new EmailTextTemplateViewModel()
                {
                    Subject = t.Value.Subject,
                    Message = t.Value.Message
                }); ;
            var defaultEmailTemplates = WebInterviewConfig.DefaultEmailTemplates.ToDictionary(t => t.Key, t =>
                new EmailTextTemplateViewModel()
                {
                    ShortTitle = GetShortTitleForEmailTemplateGroup(t.Key),
                    Title = GetTitleForEmailTemplateGroup(t.Key),
                    Subject = t.Value.Subject,
                    Message = t.Value.Message
                });

            return Ok(new
            {
                EmailTemplates = emailTemplates,
                DefaultEmailTemplates = defaultEmailTemplates,
            });
        }*/

        /*private static string GetTitleForEmailTemplateGroup(EmailTextTemplateType type)
        {
            switch (type)
            {
                case EmailTextTemplateType.InvitationTemplate: return WebInterviewSettings.ExampleInvitationEmailMessage;
                case EmailTextTemplateType.Reminder_NoResponse: return WebInterviewSettings.ExampleReminderEmailMessage;
                case EmailTextTemplateType.Reminder_PartialResponse: return WebInterviewSettings.ExampleReminderEmailMessage;
                case EmailTextTemplateType.RejectEmail: return WebInterviewSettings.ExampleRejectEmailMessage;
                default:
                    throw new ArgumentException("Unknown email template type " + type.ToString());
            }
        }

        private static string GetShortTitleForEmailTemplateGroup(EmailTextTemplateType type)
        {
            switch (type)
            {
                case EmailTextTemplateType.InvitationTemplate: return WebInterviewSettings.InvitationEmailMessage;
                case EmailTextTemplateType.Reminder_NoResponse: return WebInterviewSettings.ReminderNoResponseEmailMessage;
                case EmailTextTemplateType.Reminder_PartialResponse: return WebInterviewSettings.ReminderPartialResponseEmailMessage;
                case EmailTextTemplateType.RejectEmail: return WebInterviewSettings.RejectEmailMessage;
                default:
                    throw new ArgumentException("Unknown email template type " + type.ToString());
            }
        }*/

        public class UpdatePageTemplateModel
        {
            [Required] public WebInterviewUserMessages TitleType { get; set; }
            [Required] public string TitleText { get; set; }
            [Required] public WebInterviewUserMessages MessageType { get; set; }
            [Required] public string MessageText { get; set; }
        }

        [Route(@"{id}/pageTemplate")]
        [HttpPost]
        public IHttpActionResult UpdatePageTemplate(string id, [FromBody]UpdatePageTemplateModel updateModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return NotFound();

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
                return NotFound();

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            config.CustomMessages[updateModel.TitleType] = updateModel.TitleText;
            config.CustomMessages[updateModel.MessageType] = updateModel.MessageText;
            this.webInterviewConfigProvider.Store(questionnaireIdentity, config);

            return Ok();
        }

        public class UpdateEmailTemplateModel
        {
            [Required] public EmailTextTemplateType Type { get; set; }
            [Required] public string Subject { get; set; }
            [Required] public string Message { get; set; }
            [Required] public string PasswordDescription { get; set; }
            [Required] public string LinkText { get; set; }
        }

        [Route(@"{id}/emailTemplate")]
        [HttpPost]
        public IHttpActionResult UpdateEmailTemplate(string id, [FromBody]UpdateEmailTemplateModel updateModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return NotFound();

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
                return NotFound();

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            config.EmailTemplates[updateModel.Type] = new EmailTextTemplate(updateModel.Subject, updateModel.Message, updateModel.PasswordDescription, updateModel.LinkText);
            this.webInterviewConfigProvider.Store(questionnaireIdentity, config);

            return Ok();
        }

        public class UpdatePageMessageModel
        {
            [Required] public WebInterviewUserMessages Type { get; set; }
            [Required] public string Message { get; set; }
        }

        [Route(@"{id}/pageMessage")]
        [HttpPost]
        public IHttpActionResult UpdatePageMessage(string id, [FromBody]UpdatePageMessageModel updateModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return NotFound();

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
                return NotFound();

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            config.CustomMessages[updateModel.Type] = updateModel.Message;
            this.webInterviewConfigProvider.Store(questionnaireIdentity, config);

            return Ok();
        }


        public class UpdateAdditionalSettingsModel
        {
            [Required] public bool SpamProtection { get; set; } 
            public int? ReminderAfterDaysIfNoResponse { get; set; } 
            public int? ReminderAfterDaysIfPartialResponse { get; set; } 
        }

        [Route(@"{id}/additionalSettings")]
        [HttpPost]
        public IHttpActionResult UpdateAdditionalSettings(string id, [FromBody]UpdateAdditionalSettingsModel updateModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return NotFound();

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
                return NotFound();

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            config.UseCaptcha = updateModel.SpamProtection;
            config.ReminderAfterDaysIfNoResponse = updateModel.ReminderAfterDaysIfNoResponse;
            config.ReminderAfterDaysIfPartialResponse = updateModel.ReminderAfterDaysIfPartialResponse;
            this.webInterviewConfigProvider.Store(questionnaireIdentity, config);

            return Ok();
        }

        [Route(@"{id}/start")]
        [HttpPost]
        public IHttpActionResult Start(string id)
        {
            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return NotFound();

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
                return NotFound();

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            if (config.Started)
                return Ok();

            config.Started = true;
            config.BaseUrl = this.Url.Content("~/WebInterview");
            this.webInterviewConfigProvider.Store(questionnaireIdentity, config);

            return Ok();
        }

        [Route(@"{id}/stop")]
        [HttpPost]
        public IHttpActionResult Stop(string id)
        {
            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return NotFound();

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaire == null)
                return NotFound();

            var config = this.webInterviewConfigProvider.Get(questionnaireIdentity);
            if (!config.Started)
                return Ok();

            config.Started = false;
            this.webInterviewConfigProvider.Store(questionnaireIdentity, config);

            return Ok();
        }
    }
}
