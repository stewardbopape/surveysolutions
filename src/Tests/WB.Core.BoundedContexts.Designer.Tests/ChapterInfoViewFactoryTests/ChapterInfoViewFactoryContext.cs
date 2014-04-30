﻿using System.Collections.Generic;
using Moq;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Core.BoundedContexts.Designer.Tests.ChapterInfoViewFactoryTests
{
    internal class ChapterInfoViewFactoryContext
    {
        protected static GroupInfoView CreateChapterInfoView(string questionnaireId, string chapterId)
        {
            return new GroupInfoView()
            {
                ItemId = questionnaireId,
                Items = new List<IQuestionnaireItem>() {new GroupInfoView() {ItemId = chapterId}}
            };
        }

        protected static GroupInfoView CreateChapterInfoViewWithoutChapters(string questionnaireId, string chapterId)
        {
            return new GroupInfoView()
            {
                ItemId = questionnaireId,
                Items = new List<IQuestionnaireItem>()
            };
        }

        protected static ChapterInfoViewFactory CreateChapterInfoViewFactory(
            IQueryableReadSideRepositoryReader<GroupInfoView> repository = null)
        {
            return
                new ChapterInfoViewFactory(repository ??
                                                 Mock.Of<IQueryableReadSideRepositoryReader<GroupInfoView>>());
        }
    }
}