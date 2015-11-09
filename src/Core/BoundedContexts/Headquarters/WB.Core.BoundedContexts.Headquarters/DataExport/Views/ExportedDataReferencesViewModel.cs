﻿using System;
using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Views
{
    public class ExportedDataReferencesViewModel
    {
        public ExportedDataReferencesViewModel(
            Guid questionnaireId, 
            long questionnaireVersion, 
            ExportedDataReferencesView paraDataTabularReference,
            ExportedDataReferencesView dataTabularReference,
            ExportedDataReferencesView approvedDataTabularReference, 
            ExportedDataReferencesView dataStataReference, 
            ExportedDataReferencesView approvedDataStataReference,
            RunningDataExportProcessView[] runningProcesses)
        {
            this.QuestionnaireId = questionnaireId;
            this.QuestionnaireVersion = questionnaireVersion;
            this.ParaDataTabularReference = paraDataTabularReference;
            this.DataTabularReference = dataTabularReference;
            this.ApprovedDataTabularReference = approvedDataTabularReference;
            this.RunningProcesses = runningProcesses;
            this.DataSTATAReference = dataStataReference;
            this.ApprovedDataSTATAReference = approvedDataStataReference;
        }

        public Guid QuestionnaireId { get; private set; }
        public long QuestionnaireVersion { get; private set; }
        public ExportedDataReferencesView ParaDataTabularReference { get; private set; }
        public ExportedDataReferencesView DataTabularReference { get; private set; }
        public ExportedDataReferencesView ApprovedDataTabularReference { get; private set; }
        public ExportedDataReferencesView DataSTATAReference { get; private set; }
        public ExportedDataReferencesView ApprovedDataSTATAReference { get; private set; }
        public RunningDataExportProcessView[] RunningProcesses { get; private set; }
    }
}