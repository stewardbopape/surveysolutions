﻿using System.Collections.Generic;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Mappings;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Reports.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Infrastructure.Native.Storage;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;

namespace WB.Tests.Integration.ReportTests
{
    internal abstract class ReportContext : NpgsqlTestContext
    {
        internal ReportContext()
        {
            Sv = new SvReport(this);
            Hq = new HqReport(this);
        }

        public readonly SvReport Sv;
        public readonly HqReport Hq;

        
        protected PostgreReadSideStorage<InterviewSummary> SetupAndCreateInterviewSummaryRepository()
        {
            SetupSessionFactory();
            return CreateInterviewSummaryRepository();
        }

        protected void SetupSessionFactory()
        {
            var sessionFactory = IntegrationCreate.SessionFactory(connectionStringBuilder.ConnectionString,
                new[]
                {
                    typeof(InterviewSummaryMap),
                    typeof(TimeSpanBetweenStatusesMap),
                    typeof(QuestionAnswerMap),
                    typeof(InterviewStatisticsReportRowMap),
                    typeof(InterviewCommentedStatusMap),
                    typeof(SpeedReportInterviewItemMap)
                }, true);

            UnitOfWork = IntegrationCreate.UnitOfWork(sessionFactory);
        }

        protected PostgreReadSideStorage<InterviewSummary> CreateInterviewSummaryRepository()
        {
            return new PostgreReadSideStorage<InterviewSummary>(UnitOfWork, Mock.Of<ILogger>(), Mock.Of<IServiceLocator>());
        }

        protected PostgreReadSideStorage<SpeedReportInterviewItem> CreateSpeedReportInterviewItemsRepository()
        {
            return new PostgreReadSideStorage<SpeedReportInterviewItem>(UnitOfWork, Mock.Of<ILogger>(), Mock.Of<IServiceLocator>());
        }

        internal class SvReport
        {
            private readonly ReportContext reportContext;

            public SvReport(ReportContext reportContext)
            {
                this.reportContext = reportContext;
            }

            public TeamsAndStatusesReport TeamsAndStatuses(INativeReadSideStorage<InterviewSummary> reader = null)
            {
                if (reader == null)
                {
                    reader = reportContext.SetupAndCreateInterviewSummaryRepository();
                }
                return new TeamsAndStatusesReport(reader);
            }


            public SurveysAndStatusesReport SurveyAndStatuses(INativeReadSideStorage<InterviewSummary> reader = null)
            {
                if (reader == null)
                {
                    reader = reportContext.SetupAndCreateInterviewSummaryRepository();
                }
                return new SurveysAndStatusesReport(reader);
            }

            internal SurveysAndStatusesReport SurveyAndStatuses(List<InterviewSummary> interviews)
            {
                var reader = reportContext.SetupAndCreateInterviewSummaryRepository();
                interviews.ForEach(x => reader.Store(x, x.InterviewId.FormatGuid()));
                return SurveyAndStatuses(reader);
            }
        }

        internal class HqReport
        {
            private readonly ReportContext reportContext;

            public HqReport(ReportContext reportContext)
            {
                this.reportContext = reportContext;
            }

            public TeamsAndStatusesReport TeamsAndStatuses(INativeReadSideStorage<InterviewSummary> reader = null)
            {
                if (reader == null)
                {
                    reader = reportContext.SetupAndCreateInterviewSummaryRepository();
                }
                return new TeamsAndStatusesReport(reader);
            }

            public SurveysAndStatusesReport SurveyAndStatuses(INativeReadSideStorage<InterviewSummary> reader = null)
            {
                if (reader == null)
                {
                    reader = reportContext.SetupAndCreateInterviewSummaryRepository();
                }
                return new SurveysAndStatusesReport(reader);
            }

            public SurveysAndStatusesReport SurveyAndStatuses(List<InterviewSummary> interviews)
            {
                var reader = reportContext.SetupAndCreateInterviewSummaryRepository();
                interviews.ForEach(x => reader.Store(x, x.InterviewId.FormatGuid()));
                return SurveyAndStatuses(reader);
            }
        }
    }
}
