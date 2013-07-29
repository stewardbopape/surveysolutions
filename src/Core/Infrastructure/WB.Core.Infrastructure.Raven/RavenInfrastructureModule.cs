﻿using Main.Core;
using Ninject.Modules;
using Raven.Client.Document;

namespace WB.Core.Infrastructure.Raven
{
    public abstract class RavenInfrastructureModule : NinjectModule
    {
        private readonly RavenConnectionSettings settings;

        protected RavenInfrastructureModule(RavenConnectionSettings settings)
        {
            this.settings = settings;
        }

        protected void BindDocumentStore()
        {
            var storeProvider = new DocumentStoreProvider(this.settings);
            this.Bind<DocumentStoreProvider>().ToConstant(storeProvider);
            this.Bind<DocumentStore>().ToProvider<DocumentStoreProvider>();
        }
    }
}