﻿using System.Collections.Generic;
using System.Linq;

namespace WB.Services.Export.Interview
{
    public static class ServiceColumns
    {
        public const string ColumnDelimiter = "__";
        //Id of the row
        public static readonly string InterviewRandom = "ssSys_IRnd";
        public static readonly string HasAnyError = $"has{ColumnDelimiter}errors";
        public static readonly string Key = $"interview{ColumnDelimiter}key";
        public static readonly string InterviewId = $"interview{ColumnDelimiter}id";
        public static readonly string InterviewStatus = $"interview{ColumnDelimiter}status";
        public static readonly string ProtectedVariableNameColumn = $"variable{ColumnDelimiter}name";

        public static readonly string IdSuffixFormat = $"{{0}}{ColumnDelimiter}id";

        //prefix to identify parent record
        public const string ParentId = "ParentId";

        public const string ResponsibleColumnName = "_responsible";
        public const string AssignmentsCountColumnName = "_quantity";

        //system generated
        public static readonly SortedDictionary<ServiceVariableType, ServiceVariable> SystemVariables = new SortedDictionary<ServiceVariableType, ServiceVariable>
        {
            { ServiceVariableType.InterviewRandom,  new ServiceVariable(ServiceVariableType.InterviewRandom, InterviewRandom, 0)},
            { ServiceVariableType.InterviewKey,  new ServiceVariable(ServiceVariableType.InterviewKey, Key, 1)},
            { ServiceVariableType.HasAnyError,  new ServiceVariable(ServiceVariableType.HasAnyError, HasAnyError, 2)},
            { ServiceVariableType.InterviewStatus,  new ServiceVariable(ServiceVariableType.InterviewStatus, InterviewStatus, 3)},
        };

        public static readonly string[] AllSystemVariables = SystemVariables.Values
            .Select(x => x.VariableExportColumnName).Select(x => x.ToLower()).ToArray();
    }
}