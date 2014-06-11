﻿using System.Web.Optimization;

namespace WB.UI.Headquarters
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.UseCdn = false;

            bundles.IgnoreList.Clear();
            bundles.IgnoreList.Ignore("*-vsdoc.js");
            bundles.IgnoreList.Ignore("*intellisense.js");

            bundles.Add(new StyleBundle("~/Content/main").Include(
                "~/Content/bootstrap.css",
                "~/Content/font-awesome.min.css",
                "~/Content/bootstrap-mvc-validation.css",
                "~/Content/jquery.pnotify.default.css",
                "~/Content/app.css"));

            bundles.Add(new StyleBundle("~/css/main-not-loggedin").Include(
                "~/Content/bootstrap.css",
                "~/Content/bootstrap-mvc-validation.css",
                "~/Content/main-not-logged.css"));

            bundles.Add(new StyleBundle("~/css/admin").Include(
                "~/Content/bootstrap.css",
                "~/Content/admin.css"));

            bundles.Add(new StyleBundle("~/css/list").Include("~/Content/listview.css"));

            bundles.Add(new StyleBundle("~/css/interview-new").Include(
                "~/Content/datepicker.css"));

            bundles.Add(new StyleBundle("~/css/interview").Include(
                "~/Content/datepicker.css"));
        }
    }
}