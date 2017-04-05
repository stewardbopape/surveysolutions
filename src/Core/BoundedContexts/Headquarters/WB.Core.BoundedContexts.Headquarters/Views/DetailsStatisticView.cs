namespace WB.Core.BoundedContexts.Headquarters.Views
{
    public class DetailsStatisticView
    {
        public int FlaggedCount { get; set; }
        public int CommentedCount { get; set; }
        public int InvalidCount { get; set; }
        public int EnabledCount { get; set; }
        public int SupervisorsCount { get; set; }
        public int AnsweredCount { get; set; }
        public int AllCount { get; set; }
        public int UnansweredCount => AllCount - AnsweredCount;
        public int HiddenCount { get; set; }
    }
}