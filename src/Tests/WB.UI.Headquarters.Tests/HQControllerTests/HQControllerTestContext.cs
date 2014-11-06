﻿using System;
using System.Web;
using Main.Core.View;
using Moq;
using Ncqrs.Commanding.ServiceModel;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Services.Preloading;
using WB.Core.SharedKernels.SurveyManagement.Views.Preloading;
using WB.Core.SharedKernels.SurveyManagement.Views.TakeNew;
using WB.Core.SharedKernels.SurveyManagement.Views.User;
using WB.Core.SharedKernels.SurveyManagement.Views.UsersAndQuestionnaires;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Membership;
using WB.UI.Headquarters.Controllers;

namespace WB.UI.Headquarters.Tests.HQControllerTests
{
    internal class HqControllerTestContext
    {
        protected static QuestionnairePreloadingDataItem CreateQuestionnaireBrowseItem()
        {
            return new QuestionnairePreloadingDataItem(Guid.NewGuid(), 1,"", new QuestionDescription[0]);
        }

        protected static BatchUploadModel CreateBatchUploadModel(HttpPostedFileBase file, Guid questionnaireId)
        {
            return new BatchUploadModel
            {
                QuestionnaireId = questionnaireId,
                File = file
            };
        }

        protected static HQController CreateHqController(ISampleImportService sampleImportServiceMock = null)
        {
            return new HQController(
                Mock.Of<ICommandService>(),
                Mock.Of<IGlobalInfoProvider>(),
                Mock.Of<ILogger>(),
                Mock.Of<IViewFactory<TakeNewInterviewInputModel, TakeNewInterviewView>>(),
                Mock.Of<IViewFactory<UserListViewInputModel, UserListView>>(),
                sampleImportServiceMock ?? Mock.Of<ISampleImportService>(),
                Mock.Of<IViewFactory<AllUsersAndQuestionnairesInputModel, AllUsersAndQuestionnairesView>>(),
                Mock.Of<IPreloadingTemplateService>(), Mock.Of<IPreloadedDataRepository>(),
                Mock.Of<IPreloadedDataVerifier>(),
                Mock.Of<IViewFactory<QuestionnaireItemInputModel, QuestionnaireBrowseItem>>());
        }
    }
}