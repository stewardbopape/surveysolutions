﻿using Main.Core.View;
using Main.Core.View.Questionnaire;

namespace WB.UI.Headquarter.Controllers
{
    using System;
    using System.Web;
    using System.Web.Mvc;

    using Main.Core.Entities.SubEntities;

    using Ncqrs;
    using Ncqrs.Commanding.ServiceModel;
    using Main.Core.Commands.Questionnaire.Completed;

    public class DashboardController : Controller
    {
        private readonly IViewRepository viewRepository;

        public DashboardController(IViewRepository viewRepository)
        {
            this.viewRepository = viewRepository;
        }

        public ActionResult Questionnaires(QuestionnaireBrowseInputModel input)
        {
             var model = this.viewRepository.Load<QuestionnaireBrowseInputModel, QuestionnaireBrowseView>(input);
             return this.View(model);
        }

        public ActionResult NewSurvey(string id)
        {
            Guid key;
            if (!Guid.TryParse(id, out key))
                throw new HttpException("404");
            var newQuestionnairePublicKey = Guid.NewGuid();
            var commandService = NcqrsEnvironment.Get<ICommandService>();
            commandService.Execute(new CreateCompleteQuestionnaireCommand(newQuestionnairePublicKey, key, this.GetCurrentUser()));
            return this.RedirectToAction("Assign", "Survey", new { Id = newQuestionnairePublicKey, Template = id });
        }

        private UserLight GetCurrentUser()
        {
            return new UserLight(Guid.Empty, "#DUMMY#");
        }
    }
}
