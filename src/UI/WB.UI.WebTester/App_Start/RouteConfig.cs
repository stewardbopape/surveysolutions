﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WB.UI.WebTester
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("WebTester.All", "WebTester/Interview/{id}/{*url}", new
            {
                controller = "WebTester",
                action = "Interview"
            });


            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "WebTester", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
