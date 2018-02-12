﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ASP;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.WebInterview;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils;
using WB.Enumerator.Native.WebInterview;
using WB.Enumerator.Native.WebInterview.Models;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Resources;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [AuthorizeOr403(Roles = "Administrator, Headquarter")]
    [Filters.ObserverNotAllowed]
    [WebInterviewFeatureEnabled]
    public class WebInterviewSetupController : BaseController
    {
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IWebInterviewConfigurator configurator;
        private readonly IWebInterviewConfigProvider webInterviewConfigProvider;
        private readonly IPlainStorageAccessor<Assignment> assignments;
        private readonly IAssignmentsService assignmentsService;
        private readonly IWebInterviewNotificationService webInterviewNotificationService;

        // GET: WebInterviewSetup
        public WebInterviewSetupController(ICommandService commandService,
            ILogger logger, 
            IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IWebInterviewConfigurator configurator,
            IWebInterviewConfigProvider webInterviewConfigProvider,
            IPlainStorageAccessor<Assignment> assignments,
            IAssignmentsService assignmentsService,
            IWebInterviewNotificationService webInterviewNotificationService)
            : base(commandService, 
                  logger)
        {
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.configurator = configurator;
            this.webInterviewConfigProvider = webInterviewConfigProvider;
            this.assignments = assignments;
            this.assignmentsService = assignmentsService;
            this.webInterviewNotificationService = webInterviewNotificationService;
        }

        public ActionResult Start(string id)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var config = this.webInterviewConfigProvider.Get(QuestionnaireIdentity.Parse(id));
            if (config.Started) return RedirectToAction("Started", new {id = id});

            QuestionnaireBrowseItem questionnaire = this.FindQuestionnaire(id);
            if (questionnaire == null)
            {
                return this.HttpNotFound();
            }
            
            var model = new SetupModel();
            model.QuestionnaireTitle = questionnaire.Title;
            model.QuestionnaireFullName = string.Format(Pages.QuestionnaireNameFormat, questionnaire.Title, questionnaire.Version);
            model.QuestionnaireIdentity = new QuestionnaireIdentity(questionnaire.QuestionnaireId, questionnaire.Version);
            model.UseCaptcha = true;
            model.SurveySetupUrl = Url.Action("Index", "SurveySetup");

            model.TextOptions = Enum.GetValues(typeof(WebInterviewUserMessages)).Cast<WebInterviewUserMessages>()
                .ToDictionary(m => m.ToString().ToCamelCase(), m => m.ToUiString()).ToArray();
            model.DefaultTexts = WebInterviewConfig.DefaultMessages;
            model.TextDescriptions = Enum.GetValues(typeof(WebInterviewUserMessages)).Cast<WebInterviewUserMessages>()
                .ToDictionary(m => m, m => WebInterviewSetup.ResourceManager.GetString($"{nameof(WebInterviewUserMessages)}_{m}_Descr"));
            model.DefinedTexts = config.CustomMessages;
            

            return View(model);
        }

        [HttpPost]
        [ActionName("Start")]
        public ActionResult StartPost(string id, SetupModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            QuestionnaireBrowseItem questionnaire = this.FindQuestionnaire(id);
            if (questionnaire == null)
            {
                return this.HttpNotFound();
            }

            model.QuestionnaireTitle = questionnaire.Title;
            var questionnaireIdentity = QuestionnaireIdentity.Parse(id);

            Dictionary<WebInterviewUserMessages, string> customMessages = new Dictionary<WebInterviewUserMessages, string>();
            foreach (var customMessageName in Enum.GetValues(typeof(WebInterviewUserMessages)))
            {
                var fieldNameInRequest = customMessageName.ToString().ToCamelCase();
                if (Request[fieldNameInRequest] != null)
                {
                    customMessages[(WebInterviewUserMessages) customMessageName] = Request[fieldNameInRequest];
                }
            }

            this.configurator.Start(questionnaireIdentity, model.UseCaptcha, customMessages);
            return this.RedirectToAction("Started", new { id = questionnaireIdentity.ToString() });
        }

        public ActionResult Started(string id)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            QuestionnaireBrowseItem questionnaire = this.FindQuestionnaire(id);
            if (questionnaire == null)
            {
                return this.HttpNotFound();
            }

            var questionnaireIdentity = QuestionnaireIdentity.Parse(id);

            var model = new SetupModel
            {
                QuestionnaireTitle = questionnaire.Title,
                QuestionnaireIdentity = questionnaireIdentity
            };

            model.AssignmentsCount =
                this.assignmentsService.GetCountOfAssignmentsReadyForWebInterview(questionnaireIdentity);

            return this.View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Started")]
        public ActionResult StartedPost(string id)
        {
            var questionnaireId = QuestionnaireIdentity.Parse(id);
            this.configurator.Stop(questionnaireId);
            this.webInterviewNotificationService.ReloadInterviewByQuestionnaire(questionnaireId);

            return RedirectToAction("Index", "SurveySetup");
        }

        private QuestionnaireBrowseItem FindQuestionnaire(string id)
        {
            QuestionnaireIdentity questionnarieId;
            if (!QuestionnaireIdentity.TryParse(id, out questionnarieId))
            {
                return null;
            }

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnarieId);
            return questionnaire;
        }
    }

    public class SetupModel
    {
        public string QuestionnaireTitle { get; set; }
        public bool UseCaptcha { get; set; }
        public int AssignmentsCount { get; set; }
        public QuestionnaireIdentity QuestionnaireIdentity { get; set; }
        public string QuestionnaireFullName { get; set; }
        public string SurveySetupUrl { get; set; }
        public KeyValuePair<string, string>[] TextOptions { get; set; }
        public Dictionary<WebInterviewUserMessages, string> DefaultTexts { get; set; }
        public Dictionary<WebInterviewUserMessages, string> TextDescriptions { get; set; }
        public Dictionary<WebInterviewUserMessages, string> DefinedTexts { get; set; }
    }
}