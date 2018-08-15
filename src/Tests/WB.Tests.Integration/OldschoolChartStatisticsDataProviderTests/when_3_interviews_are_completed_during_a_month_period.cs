using System;
using FluentAssertions;
using Moq;
using NHibernate;
using Npgsql;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.EventHandler;
using WB.Core.BoundedContexts.Headquarters.Implementation.Factories;
using WB.Core.BoundedContexts.Headquarters.Mappings;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.Transactions;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using WB.Tests.Integration.PostgreSQLTests;

namespace WB.Tests.Integration.OldschoolChartStatisticsDataProviderTests
{
    internal class when_3_interviews_are_completed_during_a_month_period : with_postgres_db
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var sessionFactory = IntegrationCreate.SessionFactory(ConnectionStringBuilder.ConnectionString,
                new[] {typeof (CumulativeReportStatusChangeMap)}, true);
            postgresTransactionManager = new CqrsPostgresTransactionManager(sessionFactory ?? Mock.Of<ISessionFactory>());

            pgSqlConnection = new NpgsqlConnection(ConnectionStringBuilder.ConnectionString);
            pgSqlConnection.Open();

            cumulativeReportStatusChangeStorage =
                new PostgreReadSideStorage<CumulativeReportStatusChange>(postgresTransactionManager, Mock.Of<ILogger>());

            var cumulativeReportStatusChangeBegin = IntegrationCreate.CumulativeReportStatusChange(questionnaireId, questionnaireVersion, beginDate);
            var cumulativeReportStatusChangeInBetween = IntegrationCreate.CumulativeReportStatusChange(questionnaireId, questionnaireVersion, dateInBetween);
            var cumulativeReportStatusChangeEnd = IntegrationCreate.CumulativeReportStatusChange(questionnaireId, questionnaireVersion, endDate);

            ExecuteInCommandTransaction(() =>
            {
                cumulativeReportStatusChangeStorage.Store(cumulativeReportStatusChangeBegin,
                    cumulativeReportStatusChangeBegin.EntryId);
                cumulativeReportStatusChangeStorage.Store(cumulativeReportStatusChangeInBetween,
                    cumulativeReportStatusChangeInBetween.EntryId);
                cumulativeReportStatusChangeStorage.Store(cumulativeReportStatusChangeEnd,
                 cumulativeReportStatusChangeEnd.EntryId);
            });

            oldschoolChartStatisticsDataProvider =
                new OldschoolChartStatisticsDataProvider(cumulativeReportStatusChangeStorage);
            BecauseOf();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            pgSqlConnection.Close();
        }

        private void BecauseOf() =>
                result =
                    postgresTransactionManager.ExecuteInQueryTransaction(
                        () => oldschoolChartStatisticsDataProvider.GetStatisticsInOldFormat(questionnaireId, questionnaireVersion));

        [NUnit.Framework.Test] public void should_return_32_days_timespan () => result.StatisticsByDate.Count.Should().Be(32);


        [NUnit.Framework.Test] public void should_return_1_completed_interview_on_first_day () => result.StatisticsByDate[beginDate].CompletedCount.Should().Be(1);

        [NUnit.Framework.Test] public void should_return_2_completed_interview_on_april_first () => result.StatisticsByDate[dateInBetween].CompletedCount.Should().Be(2);

        [NUnit.Framework.Test] public void should_return_3_completed_interview_on_last_day () => result.StatisticsByDate[endDate].CompletedCount.Should().Be(3);


        private static void ExecuteInCommandTransaction(Action action)
        {
            try
            {
                postgresTransactionManager.BeginCommandTransaction();

                action();

                postgresTransactionManager.CommitCommandTransaction();
            }
            catch
            {
                postgresTransactionManager.RollbackCommandTransaction();
                throw;
            }
        }
        private static OldschoolChartStatisticsDataProvider oldschoolChartStatisticsDataProvider;
        private static StatisticsGroupedByDateAndTemplate result;
        private static CqrsPostgresTransactionManager postgresTransactionManager;
        private static Guid questionnaireId=Guid.NewGuid();
        private static long questionnaireVersion = 1;
        private static DateTime beginDate = new DateTime(2016, 3,18);
        private static DateTime dateInBetween = new DateTime(2016, 4, 1);
        private static DateTime endDate = new DateTime(2016, 4, 18);
        private static NpgsqlConnection pgSqlConnection;
        private static PostgreReadSideStorage<CumulativeReportStatusChange> cumulativeReportStatusChangeStorage;
    }
}
