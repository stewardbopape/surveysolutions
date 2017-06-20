using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Main.Core.Entities.Composite;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.Services.SynchronizationProcessTests.AssignmentTests
{
    public class when_synchronize_assignments
    {
        private List<AssignmentDocument> LocalAssignments;
        private List<AssignmentApiDocument> RemoteAssignments;
        private IAssignmentDocumentsStorage localAssignmentsRepo;
        private Mock<IProgress<SyncProgressInfo>> progressInfo;

        private void Context()
        {
            this.LocalAssignments = new List<AssignmentDocument>
            {
                Create.Entity
                    .AssignmentDocument(1, 10, 0, Create.Entity.QuestionnaireIdentity(Id.gA).ToString())
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "1")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "2")
                    .Build(),
                Create.Entity
                    .AssignmentDocument(2, 10, 0, Create.Entity.QuestionnaireIdentity(Id.gB).ToString())
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "1")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "2")
                    .Build()
            };

            this.RemoteAssignments = new List<AssignmentApiDocument>
            {
                Create.Entity
                    .AssignmentApiDocument(1, 20, Create.Entity.QuestionnaireIdentity(Id.gA))
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "1")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "2")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "3")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "4")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "gpsQuestion",latitude: 10.0, longtitude: 20.0)
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "gpsnonIdent")
                    .Build(),
                Create.Entity
                    .AssignmentApiDocument(3, 20, Create.Entity.QuestionnaireIdentity(Id.gC))
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "1")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "2")
                    .WithAnswer(Create.Entity.Identity(Guid.NewGuid()), "3")
                    .WithAnswer(Create.Entity.Identity(Id.gA), "gpsQuestion_1", latitude: 10.0, longtitude: 20.0)
                    .WithAnswer(Create.Entity.Identity(Id.gB), "gpsQuestion2_3")
                    .Build()
            };
        }

        PlainQuestionnaire CreatePlain(AssignmentApiDocument assignment)
        {
            var questionnaire = Create.Entity.QuestionnaireDocument(assignment.QuestionnaireId.QuestionnaireId, children: new IComposite[]
            {
                Create.Entity.TextQuestion(assignment.Answers[0].Identity.Id, text: "text 1"),
                Create.Entity.TextQuestion(assignment.Answers[1].Identity.Id, text: "title 2"),
                Create.Entity.TextQuestion(assignment.Answers[2].Identity.Id, text: "title 3", preFilled: true),
                Create.Entity.GpsCoordinateQuestion(assignment.Answers[3].Identity.Id, isPrefilled: true),
                Create.Entity.GpsCoordinateQuestion(assignment.Answers[4].Identity.Id)
            });

            questionnaire.Title = "title";
            return Create.Entity.PlainQuestionnaire(questionnaire);
        }

        private AssignmentApiView FromView(AssignmentApiDocument document)
        {
            return new AssignmentApiView
            {
                Id = document.Id,
                Quantity = document.Quantity,
                QuestionnaireId = document.QuestionnaireId
            };
        }

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            this.Context();

            this.localAssignmentsRepo = Create.Storage.AssignmentDocumentsInmemoryStorage();
            this.localAssignmentsRepo.Store(this.LocalAssignments);

            var assignmentSyncService = new Mock<IAssignmentSynchronizationApi>();
            assignmentSyncService.Setup(s => s.GetAssignmentsAsync(Moq.It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(this.RemoteAssignments.Select(FromView).ToList()));

            assignmentSyncService.Setup(s => s.GetAssignmentAsync(It.IsAny<int>(), Moq.It.IsAny<CancellationToken>()))
                .Returns(new Func<int, CancellationToken, Task<AssignmentApiDocument>>((id, token) =>
                {
                    var result = this.RemoteAssignments.FirstOrDefault(i => i.Id == id);

                    return Task.FromResult(result);
                }));

            var interviewViewRepository = new SqliteInmemoryStorage<InterviewView>();
            interviewViewRepository.Store(new List<InterviewView>());

            var questionarrieStorage = new Mock<IQuestionnaireStorage>();
            questionarrieStorage
                .Setup(qs => qs.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()))
                .Returns(new Func<QuestionnaireIdentity, string, IQuestionnaire>((identity, version) =>
                {
                    return CreatePlain(this.RemoteAssignments.FirstOrDefault(a => a.QuestionnaireId == identity));
                }));

            var viewModel = Create.Service.AssignmentsSynchronizer(
                synchronizationService: assignmentSyncService.Object,
                assignmentsRepository: this.localAssignmentsRepo,
                questionnaireStorage: questionarrieStorage.Object
            );

            this.progressInfo = new Mock<IProgress<SyncProgressInfo>>();

            await viewModel.SynchronizeAssignmentsAsync(progressInfo.Object, new SychronizationStatistics(), CancellationToken.None);
        }

        [Test]
        public void should_add_new_assignment()
        {
            var newRemoteAssign = this.localAssignmentsRepo.LoadAll().FirstOrDefault(ad => ad.Id == 3);
            Assert.NotNull(newRemoteAssign);
        }

        [Test]
        public void should_fill_identifying_answers_without_gps()
        {
            var assignment = this.localAssignmentsRepo.LoadAll().First(ass => ass.Id == 3);

            assignment.IdentifyingAnswers.Should().HaveCount(1);
            assignment.IdentifyingAnswers.Should().NotContain(ia => ia.Identity.Id == Id.gA);
        }

        [Test]
        public void should_remove_removed_assignment()
        {
            var newRemoteAssign = this.localAssignmentsRepo.LoadAll().FirstOrDefault(ad => ad.Id == 2);
            newRemoteAssign.Should().BeNull();
        }

        [Test]
        public void should_update_existing_assignment_quantity()
        {
            var existingAssignment = this.localAssignmentsRepo.LoadAll().FirstOrDefault(ad => ad.Id == 1);
            Assert.That(existingAssignment.Quantity, Is.EqualTo(this.RemoteAssignments[0].Quantity));
        }

        [Test]
        public void should_make_local_assignments_equal_to_remote()
        {
            var assignments = this.localAssignmentsRepo.LoadAll();

            Assert.That(assignments, Has.Count.EqualTo(this.RemoteAssignments.Count));

            var remoteLookup = this.RemoteAssignments.ToDictionary(x => x.Id);
            foreach (var local in assignments)
            {
                var remote = remoteLookup[local.Id];

                Assert.That(remote.Quantity, Is.EqualTo(local.Quantity));
                Assert.That(remote.QuestionnaireId.ToString(), Is.EqualTo(local.QuestionnaireId));
            }
        }

        [Test]
        public void should_count_new_assignments()
        {
            this.progressInfo.Verify(s => s.Report(It.Is<SyncProgressInfo>(p => p.Statistics.NewAssignmentsCount == 1)));
        }

        [Test]
        public void should_count_removed_assignments()
        {
            this.progressInfo.Verify(s => s.Report(It.Is<SyncProgressInfo>(p => p.Statistics.RemovedAssignmentsCount == 1)));
        }
    }
}