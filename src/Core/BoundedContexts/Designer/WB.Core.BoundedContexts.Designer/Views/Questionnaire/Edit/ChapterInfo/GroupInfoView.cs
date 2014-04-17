using System.Collections.Generic;
using WB.Core.Infrastructure.ReadSide.Repository;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo
{
    public class GroupInfoView : GroupInfoStatisticsView, IReadSideRepositoryEntity
    {
        public string GroupId { get; set; }
        public string Title { get; set; }
        public List<QuestionInfoView> Questions { get; set; }
        public List<GroupInfoView> Groups { get; set; }
    }
}