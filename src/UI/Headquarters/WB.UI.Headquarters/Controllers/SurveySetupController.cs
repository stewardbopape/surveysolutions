﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Repositories;
using WB.Core.BoundedContexts.Headquarters.Resources;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Infrastructure.Native.Threading;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [AuthorizeOr403(Roles = "Administrator, Headquarter")]
    public class SurveySetupController : BaseController
    {
        private readonly IPreloadingTemplateService preloadingTemplateService;
        private readonly IPreloadedDataRepository preloadedDataRepository;
        private readonly IPreloadedDataVerifier preloadedDataVerifier;
        private readonly ISampleUploadViewFactory sampleUploadViewFactory;
        private readonly InterviewDataExportSettings interviewDataExportSettings;
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IInterviewImportService interviewImportService;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IAuthorizedUser authorizedUser;
        private readonly IPreloadedDataServiceFactory preloadedDataServiceFactory;
        private readonly IQuestionnaireExportStructureStorage questionnaireExportStructureStorage;
        private readonly IRosterStructureService rosterStructureService;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IAssignmentsImportService assignmentsImportService;

        public SurveySetupController(
            ICommandService commandService,
            ILogger logger,
            IPreloadingTemplateService preloadingTemplateService,
            IPreloadedDataRepository preloadedDataRepository,
            IPreloadedDataVerifier preloadedDataVerifier,
            ISampleUploadViewFactory sampleUploadViewFactory,
            InterviewDataExportSettings interviewDataExportSettings,
            IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IInterviewImportService interviewImportService,
            IFileSystemAccessor fileSystemAccessor,
            IAuthorizedUser authorizedUser,
            IPreloadedDataServiceFactory preloadedDataServiceFactory,
            IQuestionnaireExportStructureStorage questionnaireExportStructureStorage,
            IRosterStructureService rosterStructureService,
            IQuestionnaireStorage questionnaireStorage,
            IAssignmentsImportService assignmentsImportService)
            : base(commandService, logger)
        {
            this.preloadingTemplateService = preloadingTemplateService;
            this.preloadedDataRepository = preloadedDataRepository;
            this.preloadedDataVerifier = preloadedDataVerifier;
            this.interviewDataExportSettings = interviewDataExportSettings;
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.interviewImportService = interviewImportService;
            this.fileSystemAccessor = fileSystemAccessor;
            this.authorizedUser = authorizedUser;
            this.preloadedDataServiceFactory = preloadedDataServiceFactory;
            this.questionnaireExportStructureStorage = questionnaireExportStructureStorage;
            this.rosterStructureService = rosterStructureService;
            this.questionnaireStorage = questionnaireStorage;
            this.assignmentsImportService = assignmentsImportService;
            this.sampleUploadViewFactory = sampleUploadViewFactory;
        }

        public ActionResult Index()
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            this.ViewBag.EnableInterviewHistory = this.interviewDataExportSettings.EnableInterviewHistory;
            return this.View();
        }

        public ActionResult BatchUpload(Guid id, long version)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            var featuredQuestionItems = this.sampleUploadViewFactory.Load(new SampleUploadViewInputModel(id, version)).ColumnListToPreload;
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(id, version));

            var viewModel = new BatchUploadModel
            {
                QuestionnaireId = id,
                QuestionnaireVersion = version,
                QuestionnaireTitle = questionnaireInfo?.Title,
                FeaturedQuestions = featuredQuestionItems
            };

            return this.View(viewModel);
        }

        public ActionResult TemplateDownload(Guid id, long version)
        {
            var pathToFile = this.preloadingTemplateService.GetFilePathToPreloadingTemplate(id, version);
            return this.File(this.fileSystemAccessor.ReadFile(pathToFile), "application/zip", fileDownloadName: this.fileSystemAccessor.GetFileName(pathToFile));
        }

        public ActionResult SimpleTemplateDownload(Guid id, long version)
        {
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(id, version));
            if (questionnaireInfo == null || questionnaireInfo.IsDeleted)
                return this.HttpNotFound();

            string fileName = this.fileSystemAccessor.MakeValidFileName(questionnaireInfo.Title + ".tab");
            byte[] templateFile = this.preloadingTemplateService.GetPrefilledPreloadingTemplateFile(id, version);
            return this.File(templateFile, "text/tab-separated-values", fileDownloadName: fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public ActionResult PanelBatchUploadAndVerify(BatchUploadModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            if (!this.ModelState.IsValid)
                return this.RedirectToAction(nameof(BatchUpload), new { id = model.QuestionnaireId, version = model.QuestionnaireVersion });

            var questionnaireIdentity = new QuestionnaireIdentity(model.QuestionnaireId, model.QuestionnaireVersion);
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);

            if (questionnaireInfo.IsDeleted)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_Questionnaire,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            if (@".zip" != this.fileSystemAccessor.GetFileExtension(model.File.FileName).ToLower())
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_ZipFile,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            var metaByFilesInZip = this.assignmentsImportService.ParseZipMetadata(model.File.InputStream);
            if (!metaByFilesInZip.Any())
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_FileOpen,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            var unsupportedFiles = metaByFilesInZip.Where(file => !file.CanBeHandled);
            if (unsupportedFiles.Any())
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        string.Format(
                            global::Resources.BatchUpload.Prerequisite_UnsupportedFiles,
                            string.Join(@", ", unsupportedFiles.Select(file => file.FileName))),
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }
            
            var allImportedFiles = this.assignmentsImportService.ParseZip(model.File.InputStream);
            if (allImportedFiles == null || !allImportedFiles.Any())
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        PreloadingVerificationMessages.PL0024_DataWasNotFound,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            var mainImportedFile = allImportedFiles.FirstOrDefault(d =>
                Path.GetFileNameWithoutExtension(d.FileName) == questionnaireInfo.Title);

            if (mainImportedFile == null)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        PreloadingVerificationMessages.PL0040_QuestionnaireDataIsNotFound,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            var preloadedDataService = this.CreatePreloadedDataService(questionnaireIdentity);

            this.interviewImportService.Status.VerificationState.Errors = this.preloadedDataVerifier
                .VerifyPanelFiles(allImportedFiles.ToArray(), preloadedDataService).Take(10).ToList();

            if (this.interviewImportService.Status.VerificationState.Errors.Any())
            {
                return this.View("InterviewImportVerificationErrors",
                    new ImportDataParsingErrorsView(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        this.interviewImportService.Status.VerificationState.Errors.ToArray(),
                        new InterviewImportError[0],
                        false,
                        AssignmentImportType.Panel,
                        model.File.FileName));
            }

            this.preloadedDataRepository.Store(model.File.InputStream);
            
            var importDataInfo = this.preloadedDataVerifier.GetDetails(mainImportedFile);

            this.interviewImportService.Status.VerificationState.FileName = model.File.FileName;
            this.interviewImportService.Status.QuestionnaireId = questionnaireIdentity;
            this.interviewImportService.Status.QuestionnaireTitle = questionnaireInfo.Title;
            this.interviewImportService.Status.VerificationState.SupervisorsCount = importDataInfo.SupervisorsCount;
            this.interviewImportService.Status.VerificationState.EnumeratorsCount = importDataInfo.EnumeratorsCount;
            this.interviewImportService.Status.VerificationState.EntitiesCount = importDataInfo.EntitiesCount;
            this.interviewImportService.Status.VerificationState.WasResponsibleProvided = this.preloadedDataVerifier.HasResponsibleNames(mainImportedFile);

            Task.Factory.StartNew(() =>
            {
                ThreadMarkerManager.MarkCurrentThreadAsIsolated();
                try
                {
                    this.interviewImportService.VerifyAssignments(questionnaireIdentity);
                }
                finally
                {
                    ThreadMarkerManager.ReleaseCurrentThreadFromIsolation();
                }
            });

            return this.RedirectToAction("InterviewVerificationProgress");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public ActionResult AssignmentsBatchUploadAndVerify(BatchUploadModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            if (!this.ModelState.IsValid)
                return this.RedirectToAction("BatchUpload", new { id = model.QuestionnaireId, version = model.QuestionnaireVersion });

            if (this.interviewImportService.Status.IsInProgress)
                return RedirectToAction("InterviewImportIsInProgress", new { questionnaireId = model.QuestionnaireId, version = model.QuestionnaireVersion });

            var questionnaireIdentity = new QuestionnaireIdentity(model.QuestionnaireId, model.QuestionnaireVersion);
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);

            if (questionnaireInfo.IsDeleted)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_Questionnaire,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }

            var extension = this.fileSystemAccessor.GetFileExtension(model.File.FileName).ToLower();
            if (!new[] {@".tab", @".txt", @".zip"}.Contains(extension))
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_TabOrTxtFile,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }
            
            PreloadedDataByFile preloadedSample = null;
            
            if (new[] { @".tab", @".txt" }.Contains(extension))
                preloadedSample = this.assignmentsImportService.ParseText(model.File.InputStream, model.File.FileName);

            if (@".zip" == extension)
                preloadedSample = this.assignmentsImportService.ParseZip(model.File.InputStream)?.FirstOrDefault();

            if (preloadedSample?.Content == null || preloadedSample.Content.Length == 0)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.QuestionnaireVersion,
                        questionnaireInfo?.Title,
                        PreloadingVerificationMessages.PL0024_DataWasNotFound,
                        AssignmentImportType.Assignments,
                        model.File.FileName));
            }

            var hasResponsibleNames = this.preloadedDataVerifier.HasResponsibleNames(preloadedSample);

            var preloadedDataService = this.CreatePreloadedDataService(questionnaireIdentity);

            var verificationStatus = this.preloadedDataVerifier
                .VerifyAssignmentsSample(preloadedSample, preloadedDataService)
                .Take(10)
                .ToArray();
            
            if (verificationStatus.Any())
            {
                return this.View("InterviewImportVerificationErrors", new ImportDataParsingErrorsView(
                    model.QuestionnaireId,
                    model.QuestionnaireVersion,
                    questionnaireInfo?.Title,
                    verificationStatus,
                    new InterviewImportError[0], 
                    hasResponsibleNames,
                    AssignmentImportType.Assignments,
                    preloadedSample?.FileName));
            }

            this.preloadedDataRepository.Store(model.File.InputStream);

            var importDataInfo = this.preloadedDataVerifier.GetDetails(preloadedSample);

            this.TempData[$@"InterviewImportConfirmation"] = new PreloadedDataConfirmationModel
            {
                QuestionnaireId = model.QuestionnaireId,
                Version = model.QuestionnaireVersion,
                QuestionnaireTitle = questionnaireInfo?.Title,
                WasResponsibleProvided = hasResponsibleNames,
                AssignmentImportType = AssignmentImportType.Assignments,
                FileName = preloadedSample.FileName,
                EnumeratorsCount = importDataInfo.EnumeratorsCount,
                SupervisorsCount = importDataInfo.SupervisorsCount,
                EntitiesCount = importDataInfo.EntitiesCount
            };

            return this.RedirectToAction("InterviewImportConfirmation",
                new
                {
                    questionnaireId = model.QuestionnaireId,
                    version = model.QuestionnaireVersion
                });
        }

        private IPreloadedDataService CreatePreloadedDataService(QuestionnaireIdentity questionnaireIdentity)
        {
            var questionnaire = this.questionnaireStorage.GetQuestionnaireDocument(questionnaireIdentity);
            var questionnaireExportStructure = this.questionnaireExportStructureStorage.GetQuestionnaireExportStructure(questionnaireIdentity);
            var questionnaireRosterStructure = this.rosterStructureService.GetRosterScopes(questionnaire);

            return questionnaireExportStructure == null || questionnaireRosterStructure == null || questionnaire == null
                ? null
                : this.preloadedDataServiceFactory.CreatePreloadedDataService(questionnaireExportStructure, questionnaireRosterStructure, questionnaire);
        }

        [HttpGet]
        public ActionResult InterviewImportVerificationCompleted()
        {
            AssignmentImportStatus status = this.interviewImportService.Status;

            var questionnaireId = status.QuestionnaireId.QuestionnaireId;
            var version = status.QuestionnaireId.Version;

            var title = status.QuestionnaireTitle;

            if (String.IsNullOrWhiteSpace(title))
            {
                var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(questionnaireId, version));
                title = questionnaireInfo?.Title;
            }

            if (status.IsInProgress)
            {
                return RedirectToAction("InterviewImportIsInProgress", new { questionnaireId = questionnaireId, version = version });
            }

            var verificationState = status.VerificationState;

            //clean up for security reasons
            if (verificationState.Errors.Any() || status.State.Errors.Any())
            {
                this.preloadedDataRepository.DeletePreloadedData();
                return this.View("InterviewImportVerificationErrors", new ImportDataParsingErrorsView(
                    questionnaireId,
                    version,
                    title,
                    verificationState.Errors.ToArray(),
                    status.State.Errors.ToArray(),
                    verificationState.WasResponsibleProvided,
                    AssignmentImportType.Panel,
                    verificationState.FileName));
            }

            this.TempData[$"InterviewImportConfirmation"] = new PreloadedDataConfirmationModel
            {
                QuestionnaireId = questionnaireId,
                Version = version,
                QuestionnaireTitle = status.QuestionnaireTitle,
                WasResponsibleProvided = verificationState.WasResponsibleProvided,
                AssignmentImportType = AssignmentImportType.Panel,
                FileName = verificationState.FileName,
                EnumeratorsCount = verificationState.EnumeratorsCount,
                SupervisorsCount = verificationState.SupervisorsCount,
                EntitiesCount = verificationState.EntitiesCount
            };

            return RedirectToAction("InterviewImportConfirmation", new { questionnaireId = questionnaireId, version = version });
        }

        [HttpGet]
        public ActionResult InterviewImportIsInProgress(Guid questionnaireId, long version)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;
            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(questionnaireId, version));

            return this.View(new PreloadedDataInProgressModel
            {
                Questionnaire = new PreloadedDataQuestionnaireModel
                {
                    Id = questionnaireId,
                    Version = version,
                    Title = questionnaireInfo?.Title
                }
            });
        }

        [HttpGet]
        public ActionResult InterviewImportConfirmation(Guid questionnaireId, long version)
        {
            if (this.interviewImportService.Status.IsInProgress)
            {
                return RedirectToAction("InterviewImportIsInProgress", new { questionnaireId = questionnaireId, version = version });
            }

            var key = $@"InterviewImportConfirmation";
            PreloadedDataConfirmationModel model = null;
            if (this.TempData.ContainsKey(key))
            {
                model = this.TempData[key] as PreloadedDataConfirmationModel;
            }
            if (model == null)
            {
                // load persisted state in future
                var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(new QuestionnaireIdentity(questionnaireId, version));


                if (questionnaireInfo.IsDeleted)
                {
                    return this.View("InterviewImportVerificationErrors",
                        ImportDataParsingErrorsView.CreatePrerequisiteError(
                            questionnaireId,
                            version,
                            questionnaireInfo?.Title,
                            global::Resources.BatchUpload.Prerequisite_Questionnaire,
                            AssignmentImportType.Panel,
                            null));
                }

                model = new PreloadedDataConfirmationModel
                {
                    QuestionnaireId = questionnaireId,
                    Version = version,
                    QuestionnaireTitle = questionnaireInfo?.Title
                };
            }

            return this.View(model);
        }

        [ObserverNotAllowed]
        public ActionResult InterviewVerificationProgress(Guid id)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            AssignmentImportStatus status = this.interviewImportService.Status;

            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(status.QuestionnaireId);

            if (questionnaireInfo == null)
            {
                return RedirectToAction("Index");
            }

            return this.View(new PreloadedDataInterviewProgressModel
            {
                Status = status,
                QuestionnaireId = questionnaireInfo.QuestionnaireId,
                Version = questionnaireInfo.Version,
                QuestionnaireTitle = questionnaireInfo.Title,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public ActionResult InterviewImportConfirmation(PreloadedDataConfirmationModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            if (this.interviewImportService.Status.IsInProgress)
            {
                return RedirectToAction("InterviewImportIsInProgress", new { questionnaireId = model.QuestionnaireId, version = model.Version });
            }

            var questionnaireIdentity = new QuestionnaireIdentity(model.QuestionnaireId, model.Version);
            QuestionnaireBrowseItem questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);

            if (questionnaireInfo.IsDeleted)
            {
                return this.View("InterviewImportVerificationErrors",
                    ImportDataParsingErrorsView.CreatePrerequisiteError(
                        model.QuestionnaireId,
                        model.Version,
                        questionnaireInfo?.Title,
                        global::Resources.BatchUpload.Prerequisite_Questionnaire,
                        model.AssignmentImportType,
                        null));
            }

            this.interviewImportService.Status.QuestionnaireId = questionnaireIdentity;
            this.interviewImportService.Status.QuestionnaireTitle = questionnaireInfo.Title;
            
            if (!this.ModelState.IsValid)
            {
                model.QuestionnaireTitle = questionnaireInfo.Title;
                return this.View(model);
            }

            var headquartersId = this.authorizedUser.Id;

            Task.Factory.StartNew(() =>
            {
                ThreadMarkerManager.MarkCurrentThreadAsIsolated();

                try
                {
                    this.interviewImportService.ImportAssignments(supervisorId: model.ResponsibleId,
                        questionnaireIdentity: questionnaireIdentity,
                        headquartersId: headquartersId,
                        mode: model.AssignmentImportType,
                        shouldSkipInterviewCreation: questionnaireInfo.AllowAssignments);
                }
                finally
                {
                    this.preloadedDataRepository.DeletePreloadedData();

                    ThreadMarkerManager.ReleaseCurrentThreadFromIsolation();
                }
            });

            return this.RedirectToAction("InterviewImportProgress");
        }

        [ObserverNotAllowed]
        public ActionResult InterviewImportProgress()
        {
            this.ViewBag.ActivePage = MenuItem.Questionnaires;

            AssignmentImportStatus status = this.interviewImportService.Status;

            var questionnaireInfo = this.questionnaireBrowseViewFactory.GetById(status.QuestionnaireId);

            if (questionnaireInfo == null)
            {
                return RedirectToAction("Index");
            }

            return this.View(new PreloadedDataInterviewProgressModel
            {
                Status = status,
                QuestionnaireId = questionnaireInfo.QuestionnaireId,
                Version = questionnaireInfo.Version,
                QuestionnaireTitle = questionnaireInfo.Title,
            });
        }
    }
}