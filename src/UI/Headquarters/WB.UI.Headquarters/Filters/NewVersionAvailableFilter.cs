﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.Transactions;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Models.VersionCheck;

namespace WB.UI.Headquarters.Filters
{
    public class NewVersionAvailableFilter : ActionFilterAttribute
    {
        private static VersionCheckingInfo AvailableVersion = null;
        private static bool IsCheckingNow;

        private static DateTime? LastLoadedAt = null;
        private static int SecondsForCacheIsValid = 60*60;

        private static DateTime? ErrorOccuredAt = null;
        private static int DelayOnErrorInSeconds = 3*60;

        private IPlainKeyValueStorage<VersionCheckingInfo> versionCheckInfoStorage =>
            ServiceLocator.Current.GetInstance<IPlainKeyValueStorage<VersionCheckingInfo>>();

        private IPlainTransactionManager PlainTransactionManager =>
            ServiceLocator.Current.GetInstance<IPlainTransactionManagerProvider>().GetPlainTransactionManager();

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);

            var viewResult = filterContext.Result as ViewResult;
            if (viewResult == null) return;

            if (ApplicationSettings.NewVersionCheckEnabled && !string.IsNullOrEmpty(ApplicationSettings.NewVersionCheckUrl))
            {
                if (AvailableVersion == null)
                {
                    AvailableVersion = this.versionCheckInfoStorage.GetById(VersionCheckingInfo.StorageKey);
                }

                viewResult.ViewBag.LastAvailableApplicationVersion = AvailableVersion;

                var isCacheExpired = (DateTime.Now - LastLoadedAt)?.Seconds > SecondsForCacheIsValid;

                if (!IsCheckingNow && (LastLoadedAt == null || isCacheExpired))
                {
                    var isErrorDelayExpired = ErrorOccuredAt != null && (DateTime.Now - ErrorOccuredAt)?.Seconds > DelayOnErrorInSeconds;

                    if ((ErrorOccuredAt == null) || isErrorDelayExpired)
                    {
                        Task.Run(async () =>
                        {
                            await this.UpdateVersion();
                        });
                    }
                }
            }
        }

        private async Task UpdateVersion()
        {
            try
            {
                IsCheckingNow = true;

                var versionInfo = await DoRequestJsonAsync<VersionCheckingInfo>(ApplicationSettings.NewVersionCheckUrl);

                AvailableVersion = versionInfo;
                LastLoadedAt = DateTime.Now;
                ErrorOccuredAt = null;

                try
                {
                    this.PlainTransactionManager.BeginTransaction();
                    this.versionCheckInfoStorage.Store(versionInfo, VersionCheckingInfo.StorageKey);
                    this.PlainTransactionManager.CommitTransaction();
                }
                catch 
                {
                    this.PlainTransactionManager.RollbackTransaction();
                }
                
            }
            catch (Exception)
            {
                ErrorOccuredAt = DateTime.Now;
            }
            finally
            {
                IsCheckingNow = false;
            }
        }

        private async Task<T> DoRequestJsonAsync<T>(string uri)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
               
                var  jsonData = await httpClient.GetStringAsync(uri);
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
        }
    }
}