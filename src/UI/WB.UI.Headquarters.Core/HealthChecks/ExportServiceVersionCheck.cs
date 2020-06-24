﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WB.Core.BoundedContexts.Headquarters.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.GenericSubdomains.Portable;
using WB.UI.Headquarters.Resources;

namespace WB.UI.Headquarters.HealthChecks
{
    public class ExportServiceVersionCheck : IHealthCheck
    {
        private readonly IExportServiceApi exportServiceApi;
        private readonly IOptions<DataExportOptions> exportOptions;

        public ExportServiceVersionCheck(IOptions<DataExportOptions> exportOptions,
            IExportServiceApi exportServiceApi)
        {
            this.exportOptions = exportOptions;
            this.exportServiceApi = exportServiceApi;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var uri = this.exportOptions.Value.ExportServiceUrl + "/.version";
            try
            {
                var version = await this.exportServiceApi.Version();
                return HealthCheckResult.Healthy(Diagnostics.export_service_check_Healthy.FormatString(uri, version));
            }
            catch (Exception e)
            {
                return HealthCheckResult.Degraded(Diagnostics.export_service_connectivity_check_Degraded.FormatString(uri), e);
            }
        }
    }
}
