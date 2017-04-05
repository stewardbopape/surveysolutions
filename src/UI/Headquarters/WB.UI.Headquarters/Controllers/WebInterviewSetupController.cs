﻿using System;
using System.Web.Mvc;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.WebInterview;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models.WebInterview;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [Authorize(Roles = "Administrator, Headquarter")]
    [ObserverNotAllowed]
    [WebInterviewFeatureEnabled]
    public class WebInterviewSetupController : BaseController
    {
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IWebInterviewConfigurator configurator;
        private readonly IWebInterviewConfigProvider webInterviewConfigProvider;

        // GET: WebInterviewSetup
        public WebInterviewSetupController(ICommandService commandService,
            ILogger logger, 
            IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IWebInterviewConfigurator configurator,
            IWebInterviewConfigProvider webInterviewConfigProvider)
            : base(commandService, 
                  logger)
        {
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.configurator = configurator;
            this.webInterviewConfigProvider = webInterviewConfigProvider;
        }

        public ActionResult Start(string id)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var config = this.webInterviewConfigProvider.Get(QuestionnaireIdentity.Parse(id));
            if (config.Started) return RedirectToAction("Started", new {id = id});

            QuestionnaireBrowseItem questionnaire = this.FindCensusQuestionnaire(id);
            if (questionnaire == null)
            {
                return this.HttpNotFound();
            }
            
            var model = new SetupModel();
            model.QuestionnaireTitle = questionnaire.Title;
            model.QuestionnaireVersion = questionnaire.Version;
            model.UseCaptcha = true;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Start")]
        public ActionResult StartPost(string id, SetupModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            QuestionnaireBrowseItem questionnaire = this.FindCensusQuestionnaire(id);
            if (questionnaire == null)
            {
                return this.HttpNotFound();
            }

            model.QuestionnaireTitle = questionnaire.Title;
            model.QuestionnaireVersion = questionnaire.Version;

            if (model.ResponsibleId.HasValue)
            {
                var questionnaireIdentity = QuestionnaireIdentity.Parse(id);
                this.configurator.Start(questionnaireIdentity, model.ResponsibleId.Value, model.UseCaptcha);
                return RedirectToAction("Started", new {id = questionnaireIdentity.ToString()});
            }

            return View(model);
        }

        public ActionResult Started(string id)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            QuestionnaireBrowseItem questionnaire = this.FindCensusQuestionnaire(id);
            if (questionnaire == null)
            {
                return this.HttpNotFound();
            }

            var model = new SetupModel();
            model.QuestionnaireTitle = questionnaire.Title;
            model.QuestionnaireVersion = questionnaire.Version;

            model.WebInterviewLink = Url.Action("Start", "WebInterview", new {id=""}, Request.Url.Scheme) + "/" + QuestionnaireIdentity.Parse(id);

            return this.View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Started")]
        public ActionResult StartedPost(string id)
        {
            var questionnaireId = QuestionnaireIdentity.Parse(id);
            this.configurator.Stop(questionnaireId);

            return RedirectToAction("Index", "SurveySetup");
        }

        private QuestionnaireBrowseItem FindCensusQuestionnaire(string id)
        {
            QuestionnaireIdentity questionnarieId;
            if (!QuestionnaireIdentity.TryParse(id, out questionnarieId))
            {
                return null;
            }

            QuestionnaireBrowseItem questionnaire = this.questionnaireBrowseViewFactory.GetById(questionnarieId);
            if (questionnaire != null && questionnaire.AllowCensusMode)
            {
                return questionnaire;
            }

            return null;
        }
    }
}