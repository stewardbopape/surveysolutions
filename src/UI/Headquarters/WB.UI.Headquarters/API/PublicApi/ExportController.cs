﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Main.Core.Entities.SubEntities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WB.Core.BoundedContexts.Headquarters.DataExport.Accessors;
using WB.Core.BoundedContexts.Headquarters.DataExport.DataExportDetails;
using WB.Core.BoundedContexts.Headquarters.DataExport.Ddi;
using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;
using WB.Core.BoundedContexts.Headquarters.DataExport.Security;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services;
using WB.Core.BoundedContexts.Headquarters.DataExport.Views;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.UI.Headquarters.API.Filters;
using WB.UI.Headquarters.Code;
using WB.UI.Shared.Web.Configuration;

namespace WB.UI.Headquarters.API.PublicApi
{
    /// <summary>
    /// Provides a methods for managing export related actions
    /// </summary>
    [ApiBasicAuth(new[] { UserRoles.ApiUser, UserRoles.Administrator }, TreatPasswordAsPlain = true)]
    [RoutePrefix(@"api/v1/export")]
    public class ExportController : ApiController
    {
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IDataExportProcessesService dataExportProcessesService;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IDdiMetadataAccessor ddiMetadataAccessor;
        private readonly IFilebasedExportedDataAccessor filebasedExportedDataAccessor;
        private readonly IPlainKeyValueStorage<ExportServiceSettings> exportServiceSettings;
        private readonly IConfigurationManager configurationManager;
        private readonly IDataExportStatusReader dataExportStatusReader;

        public ExportController(IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IDataExportProcessesService dataExportProcessesService,
            IFileSystemAccessor fileSystemAccessor,
            IDdiMetadataAccessor ddiMetadataAccessor,
            IFilebasedExportedDataAccessor filebasedExportedDataAccessor,
            IPlainKeyValueStorage<ExportServiceSettings> exportServiceSettings,
            IConfigurationManager configurationManager,
            IDataExportStatusReader dataExportStatusReader)
        {
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.dataExportProcessesService = dataExportProcessesService;
            this.fileSystemAccessor = fileSystemAccessor;
            this.ddiMetadataAccessor = ddiMetadataAccessor;
            this.filebasedExportedDataAccessor = filebasedExportedDataAccessor;
            this.exportServiceSettings = exportServiceSettings;
            this.configurationManager = configurationManager;
            this.dataExportStatusReader = dataExportStatusReader;
        }

        /// <summary>
        /// Downloads export file
        /// </summary>
        /// <param name="id">Questionnaire id in format [QuestionnaireGuid$Version]</param>
        /// <param name="exportType">Format of export data to download</param>
        /// <param name="status">Status of exported interviews</param>
        /// <param name="from">Started date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// <param name="to">Finished date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// 
        /// <response code="200">Returns content of the export file as zip archrive</response>
        /// <response code="404">Export file was not generated yet</response>
        /// <response code="400">Questionnaire id is malformed</response>
        [HttpGet]
        [Route(@"{exportType}/{id}")]
        public IHttpActionResult Get(string id, DataExportFormat exportType, InterviewStatus? status = null, DateTime? from = null, DateTime? to = null)
        {
            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return this.Content(HttpStatusCode.BadRequest, @"Invalid questionnaire identity");

            string exportedFilePath;
            switch (exportType)
            {
                case DataExportFormat.DDI:
                    exportedFilePath = this.ddiMetadataAccessor.GetFilePathToDDIMetadata(questionnaireIdentity);
                    break;
                default:
                    exportedFilePath = this.filebasedExportedDataAccessor.GetArchiveFilePathForExportedData(
                        questionnaireIdentity, exportType, status, from, to);
                    break;
            }

            if(!this.fileSystemAccessor.IsFileExists(exportedFilePath))
                return this.NotFound();

            var exportedFileName = this.fileSystemAccessor.GetFileName(exportedFilePath);

            return new ProgressiveDownloadResult(this.Request, exportedFilePath, exportedFileName, @"application/zip");
        }

        /// <summary>
        /// Starts export file creation
        /// </summary>
        /// <param name="id">Questionnaire id in format [QuestionnaireGuid$Version]</param>
        /// <param name="exportType">Format of export data to download</param>
        /// <param name="status">Status of exported interviews</param>
        /// <param name="from">Started date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// <param name="to">Finished date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// 
        /// <response code="200">Export started</response>
        /// <response code="400">Questionnaire id is malformed</response>
        /// <response code="404">Questionnaire was not found</response>
        [HttpPost]
        [Route(@"{exportType}/{id?}/start")]
        public async Task<IHttpActionResult> StartProcess(string id, DataExportFormat exportType, InterviewStatus? status = null, DateTime? from = null, DateTime? to = null)
        {
            switch (exportType)
            {
                case DataExportFormat.DDI:
                    return this.BadRequest(@"Not supported export type");
                default:
                    if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                        return this.Content(HttpStatusCode.BadRequest, @"Invalid questionnaire identity");

                    var questionnaireBrowseItem = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
                    if (questionnaireBrowseItem == null)
                        return this.Content(HttpStatusCode.NotFound, @"Questionnaire not found");

                    await this.dataExportProcessesService.AddDataExportAsync(configurationManager.AppSettings["BaseUrl"], this.exportServiceSettings.GetById(AppSetting.ExportServiceStorageKey).Key, new DataExportProcessDetails(exportType, questionnaireIdentity, questionnaireBrowseItem.Title)
                    {
                        FromDate = @from,
                        ToDate = to,
                        InterviewStatus = status
                    });
                    break;
            }

            return this.Ok();
        }

        /// <summary>
        /// Stops export file creation if one is in progress 
        /// </summary>
        /// <param name="id">Questionnaire id in format [QuestionnaireGuid$Version]</param>
        /// <param name="exportType">Format of export data to download</param>
        /// <param name="status">Status of exported interviews</param>
        /// <param name="from">Started date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// <param name="to">Finished date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// 
        /// <response code="200">Canceled</response>
        /// <response code="400">Questionnaire id is malformed</response>
        /// <response code="404">Questionnaire was not found</response>
        [HttpPost]
        [Route(@"{exportType}/{id}/cancel")]
        public IHttpActionResult CancelProcess(string id, DataExportFormat exportType, InterviewStatus? status = null, DateTime? from = null, DateTime? to = null)
        {
            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return this.Content(HttpStatusCode.BadRequest, @"Invalid questionnaire identity");

            var questionnaireBrowseItem = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaireBrowseItem == null)
                return this.Content(HttpStatusCode.NotFound, @"Questionnaire not found");

            this.dataExportProcessesService.DeleteDataExport(
                new DataExportProcessDetails(exportType, questionnaireIdentity, questionnaireBrowseItem.Title)
                {
                    FromDate = from,
                    ToDate = to,
                    InterviewStatus = status
                }.NaturalId);

            return this.Ok();
        }

        /// <summary>
        /// Gets info about currently running exports
        /// </summary>
        /// <param name="id">Questionnaire id in format [QuestionnaireGuid$Version]</param>
        /// <param name="exportType">Format of export data to download</param>
        /// <param name="status">Status of exported interviews</param>
        /// <param name="from">Started date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// <param name="to">Finished date for timeframe of exported interviews (when change was done to an interview). Should be in UTC date</param>
        /// 
        /// <response code="200"></response>
        /// <response code="400">Questionnaire id is malformed</response>
        /// <response code="404">Questionnaire was not found</response>
        [HttpGet]
        [Route(@"{exportType}/{id}/details")]
        [ResponseType(typeof(ExportDetails))]
        public async Task<IHttpActionResult> ProcessDetails(string id, DataExportFormat exportType, InterviewStatus? status = null, DateTime? from = null, DateTime? to = null)
        {
            if (!QuestionnaireIdentity.TryParse(id, out var questionnaireIdentity))
                return this.Content(HttpStatusCode.BadRequest, @"Invalid questionnaire identity");

            var questionnaireBrowseItem = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);
            if (questionnaireBrowseItem == null)
                return this.Content(HttpStatusCode.NotFound, @"Questionnaire not found");


            var allExportStatuses = await this.dataExportStatusReader.GetDataExportStatusForQuestionnaireAsync(configurationManager.AppSettings["BaseUrl"],
                this.exportServiceSettings.GetById(AppSetting.ExportServiceStorageKey).Key, questionnaireIdentity);

            var exportStatusByExportType = allExportStatuses?.DataExports?.FirstOrDefault(x =>
                x.DataExportFormat == exportType);

            if (exportStatusByExportType == null)
                return this.NotFound();

            var runningExportStatus = allExportStatuses.RunningDataExportProcesses.FirstOrDefault(x =>
                x.QuestionnaireIdentity.Equals(questionnaireIdentity) && x.Format == exportType && x.FromDate == from &&
                x.ToDate == to && x.InterviewStatus == status);

            return this.Ok(new ExportDetails
            {
                HasExportedFile = exportStatusByExportType.HasDataToExport,
                LastUpdateDate = exportStatusByExportType.LastUpdateDate,
                ExportStatus = exportStatusByExportType.StatusOfLatestExportProcess,
                RunningProcess = runningExportStatus == null ? null : new RunningProcess
                {
                    StartDate = runningExportStatus.BeginDate,
                    ProgressInPercents = runningExportStatus.Progress
                }
            });
        }

        public class ExportDetails
        {
            [Required]
            public bool HasExportedFile { get; set; }
            public DateTime? LastUpdateDate { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [Required]
            public DataExportStatus ExportStatus { get; set; }
            public RunningProcess RunningProcess { get; set; }
        }

        public class RunningProcess
        {
            [Required]
            public DateTime StartDate { get; set; }
            [Required]
            public int ProgressInPercents { get; set; }
        }
    }
}
