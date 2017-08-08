﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;
using Microsoft.Owin.BuilderProperties;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security.Cookies;
using NConfig;
using Ninject.Web.Common;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using NLog;
using Owin;
using Quartz;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.Versions;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Resources;
using WB.UI.Shared.Web.Configuration;
using WB.UI.Shared.Web.DataAnnotations;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Headquarters
{
    public class Startup
    {
        internal static void SetupNConfig()
        {
            NConfigurator.RegisterSectionMerger(new DeepMerger<HqSecuritySection>());
            NConfigurator.UsingFiles(@"~\Configuration\Headquarters.Web.config").SetAsSystemDefault();
        }

        static Startup()
        {
            SetupNConfig();
            //HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
        }

        public void Configuration(IAppBuilder app)
        {
            app.Use(RemoveServerNameFromHeaders);

            ConfigureNinject(app);
            var logger = ServiceLocator.Current.GetInstance<ILoggerProvider>().GetFor<Startup>();
            logger.Info($@"Starting Headquarters {ServiceLocator.Current.GetInstance<IProductVersion>()}");
            UpdateAppVersion();
            ConfigureAuth(app);
            InitializeAppShutdown(app);
            InitializeMVC();
            ConfigureWebApi(app);
        }

        private void ConfigureNinject(IAppBuilder app)
        {
            var perRequestModule = new OnePerRequestHttpModule();

            // onPerRequest scope implementation. Collecting all perRequest instances after all requests
            app.Use(async (ctx, next) =>
            {
                try
                {
                    if (ctx.Request.CallCancelled.IsCancellationRequested) return;

                    await next();
                }
                finally
                {
                    perRequestModule.DeactivateInstancesForCurrentHttpRequest();
                }
            });

            var kernel = NinjectConfig.CreateKernel();
            kernel.Inject(perRequestModule); // wiill keep reference to perRequestModule in Kernel instance
            app.UseNinjectMiddleware(() => kernel);
        }

        internal class SignalRHubMinifier : IJavaScriptMinifier
        {
            readonly ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

            public string Minify(string source)
            {
                return this.cache.GetOrAdd(source, s => new Minifier().MinifyJavaScript(source, new CodeSettings
                {
                    PreserveImportantComments = false
                }));
            }
        }

        private void ConfigureWebApi(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.Formatters.Add(new FormMultipartEncodedMediaTypeFormatter());

            GlobalConfiguration.Configure(WebApiConfig.Register);
            WebApiConfig.Register(config);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GlobalHost.DependencyResolver.Register(typeof(IJavaScriptMinifier), () => new SignalRHubMinifier());
            app.MapSignalR(new HubConfiguration {EnableDetailedErrors = true});
            app.Use(SetSessionStateBehavior).UseStageMarker(PipelineStage.MapHandler);

            app.UseNinjectWebApi(config);
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            var applicationSecuritySection = NConfigurator.Default.GetSection<HqSecuritySection>(@"applicationSecurity");

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString(@"/Account/LogOn"),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator
                        .OnValidateIdentity<HqUserManager, HqUser, Guid>(
                            validateInterval: TimeSpan.FromMinutes(30),
                            regenerateIdentityCallback: (manager, user) => manager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie),
                            getUserIdCallback: (id) => Guid.Parse(id.GetUserId())),

                    OnApplyRedirect = ctx =>
                    {
                        if (!IsAjaxRequest(ctx.Request) && !IsApiRequest(ctx.Request) && !IsBasicAuthApiUnAuthRequest(ctx.Response))
                        {
                            ctx.Response.Redirect(ctx.RedirectUri);
                        }
                    }
                },
                ExpireTimeSpan = TimeSpan.FromHours(applicationSecuritySection.CookieSettings.ExpirationTime),
                SlidingExpiration = applicationSecuritySection.CookieSettings.SlidingExpiration,
                CookieName = applicationSecuritySection.CookieSettings.Name,
                CookieHttpOnly = applicationSecuritySection.CookieSettings.HttpOnly
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
        }

        private static bool IsApiRequest(IOwinRequest request)
        {
            var userAgent = request.Headers[@"User-Agent"];
            return (userAgent?.ToLowerInvariant().Contains(@"org.worldbank.solutions.") ?? false) || (userAgent?.Contains(@"okhttp/") ?? false);
                   (userAgent?.Contains(@"okhttp/") ?? false);
        }

        private static bool IsBasicAuthApiUnAuthRequest(IOwinResponse response)
        {
            return response.Headers[ApiBasicAuthAttribute.AuthHeader] != null;
        }

        private static bool IsAjaxRequest(IOwinRequest request)
        {
            IReadableStringCollection query = request.Query;
            if ((query != null) && (query["X-Requested-With"] == "XMLHttpRequest"))
            {
                return true;
            }
            IHeaderDictionary headers = request.Headers;
            return ((headers != null) && (headers["X-Requested-With"] == "XMLHttpRequest"));
        }

        private static Task SetSessionStateBehavior(IOwinContext context, Func<Task> next)
        {
            // Depending on the handler the request gets mapped to, session might not be enabled. Force it on.
            HttpContextBase httpContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
            httpContext.SetSessionStateBehavior(SessionStateBehavior.Required);
            return next();
        }

        private static Task RemoveServerNameFromHeaders(IOwinContext context, Func<Task> next)
        {
            context.Response.Headers.Remove(@"Server");
            return next.Invoke();
        }

        private static void InitializeAppShutdown(IAppBuilder app)
        {
            var properties = new AppProperties(app.Properties);
            CancellationToken token = properties.OnAppDisposing;
            if (token != CancellationToken.None)
                token.Register(OnShutdown);
        }

        private static void OnShutdown()
        {
            var logger = LogManager.GetCurrentClassLogger();

            logger.Info(@"Ending application.");
            logger.Info(@"ShutdownReason: " + HostingEnvironment.ShutdownReason);

            ServiceLocator.Current.GetInstance<IScheduler>()?.Shutdown();

            if (HostingEnvironment.ShutdownReason != ApplicationShutdownReason.HostingEnvironment) return;

            var httpRuntimeType = typeof(HttpRuntime);
            var httpRuntime = httpRuntimeType.InvokeMember(
                "_theRuntime",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField,
                null, null, null) as HttpRuntime;

            var shutDownMessage = httpRuntimeType.InvokeMember(
                "_shutDownMessage",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                null, httpRuntime, null) as string;

            string shutDownStack = httpRuntimeType.InvokeMember("_shutDownStack",
                BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.GetField,
                null,
                httpRuntime,
                null) as string;

            logger.Info(@"ShutDownMessage: " + shutDownMessage);
            logger.Info(@"ShutDownStack: " + shutDownStack);
        }

        private static void InitializeMVC()
        {
            MvcHandler.DisableMvcResponseHeader = true;

            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterHttpFilters(GlobalConfiguration.Configuration.Filters);
            RegisterWebApiFilters(GlobalConfiguration.Configuration.Filters);

            DataAnnotationsConfig.RegisterAdapters();

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());
            ValueProviderFactories.Factories.Add(new JsonValueProviderFactory());
        }

        private static void UpdateAppVersion()
            => ServiceLocator.Current.GetInstance<IProductVersionHistory>().RegisterCurrentVersion();

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new NlogExceptionFilter());
            filters.Add(new RequireSecureConnectionAttribute());
            filters.Add(new NoCacheAttribute());
            filters.Add(new MaintenanceFilter());
            filters.Add(new InstallationAttribute(), 100);
        }

        public static void RegisterHttpFilters(HttpFilterCollection filters)
        {
            filters.Add(new ElmahHandledErrorLoggerFilter());
        }

        public static void RegisterWebApiFilters(HttpFilterCollection filters)
        {
            filters.Add(new ApiMaintenanceFilter());
        }
    }
}