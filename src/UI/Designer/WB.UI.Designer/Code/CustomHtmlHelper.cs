﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomHtmlHelper.cs" company="">
//   
// </copyright>
// <summary>
//   The custom html helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WB.UI.Designer
{
    using System;
    using System.Security.Policy;
    using System.Web.Mvc;
    using System.Web.Mvc.Html;

    using Main.Core.Utility;

    /// <summary>
    /// The custom html helper.
    /// </summary>
    public static class CustomHtmlHelper
    {
        #region Public Methods and Operators

        /// <summary>
        /// The if.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="evaluation">
        /// The evaluation.
        /// </param>
        /// <returns>
        /// The <see cref="MvcHtmlString"/>.
        /// </returns>
        public static MvcHtmlString If(this MvcHtmlString value, bool evaluation)
        {
            return evaluation ? value : MvcHtmlString.Empty;
        }

        /// <summary>
        /// The menu action link.
        /// </summary>
        /// <param name="helper">
        /// The helper.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <param name="controller">
        /// The controller.
        /// </param>
        /// <returns>
        /// The <see cref="MvcHtmlString"/>.
        /// </returns>
        public static MvcHtmlString MenuActionLink(
            this HtmlHelper helper, string title, string action, string controller)
        {
            var li = new TagBuilder("li");
            if (action.Compare(GlobalHelper.CurrentAction) && controller.Compare(GlobalHelper.CurrentController))
            {
                li.AddCssClass("active");
            }
            
            li.InnerHtml = helper.ActionLink(title, action, controller).ToHtmlString();

            return MvcHtmlString.Create(li.ToString());
        }

        #endregion
    }
}