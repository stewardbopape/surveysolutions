﻿using System;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Client.Extensions;

namespace WB.Core.Infrastructure.Storage.Raven
{
    public static class RavenExtensions
    {
        public static void DeleteDatabase(this IDocumentStore ravenStore, string databaseName, bool hardDelete = false)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException("databaseName");

            var databaseCommands = ravenStore.DatabaseCommands;
            var relativeUrl = "/admin/databases/" + databaseName;

            if (hardDelete)
                relativeUrl += "?hard-delete=true";

            try
            {
                var serverClient = databaseCommands.ForSystemDatabase() as ServerClient;

                var httpJsonRequest = serverClient.CreateRequest("DELETE", relativeUrl);
                httpJsonRequest.ExecuteRequest();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to delete '{0}' database", databaseName), ex);
            }
        }

        public static void CreateDatabase(this IDocumentStore ravenStore, string databaseName)
        {
            try
            {
                ravenStore.DatabaseCommands.EnsureDatabaseExists(databaseName);
            }
            catch (Exception ex)
            {

                throw new Exception(string.Format("Failed to create '{0}' database", databaseName), ex);
            }

        }

        public static void ActivateBundles(this IDocumentStore documentStore, string activeBundles, string database = null)
        {
            if (string.IsNullOrEmpty(activeBundles))
                return;
            var bundles = activeBundles.Split(';');
            foreach (var bundle in bundles)
            {
                documentStore.ActivateBundle(bundle, database);
            }
        }

        public static void ActivateBundle(this IDocumentStore documentStore, string bundleName, string databaseName)
        {
            using (var session = documentStore.OpenSession())
            {
                var databaseDocument = session.Load<DatabaseDocument>("Raven/Databases/" + databaseName);
                if (databaseDocument == null)
                    return;
                var databaseDocumentSettings = databaseDocument.Settings;
                var activeBundles = databaseDocumentSettings.ContainsKey(Constants.ActiveBundles) ? databaseDocumentSettings[Constants.ActiveBundles] : null;
                if (string.IsNullOrEmpty(activeBundles))
                    databaseDocumentSettings[Constants.ActiveBundles] = bundleName;
                else if (!activeBundles.Split(';').Contains(bundleName, StringComparer.OrdinalIgnoreCase))
                    databaseDocumentSettings[Constants.ActiveBundles] = activeBundles + ";" + bundleName;

                session.SaveChanges();
            }
        }
    }
}
