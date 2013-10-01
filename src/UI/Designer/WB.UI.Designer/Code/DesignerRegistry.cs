﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using Main.Core;
using Ncqrs;
using Ninject;
using Ninject.Activation;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.Infrastructure.Raven.Implementation.ReadSide.RepositoryAccessors;
using WB.UI.Designer.Providers.CQRS.Accounts;
using WB.UI.Designer.Views.Questionnaire;

namespace WB.UI.Designer.Code
{
    using System;
    using System.Web.Mvc;

    using WB.UI.Designer.Filters;
    using WB.UI.Designer.WebServices;

    public class DesignerRegistry : CoreRegistry
    {
        protected override IEnumerable<Assembly> GetAssembliesForRegistration()
        {
            return
                base.GetAssembliesForRegistration()
                    .Concat(new[]
                    {
                        typeof(QuestionnaireView).Assembly, 
                        typeof(DesignerRegistry).Assembly,
                        typeof(AccountAR).Assembly,
                        typeof(PublicService).Assembly,
                        typeof(Questionnaire).Assembly,
                    });
        }

        protected override object GetReadSideRepositoryWriter(IContext context)
        {
            return ShouldUsePersistentReadLayer()
                ? this.Kernel.Get(typeof(RavenReadSideRepositoryWriter<>).MakeGenericType(context.GenericArguments[0]))
                : this.GetInMemoryReadSideRepositoryAccessor(context);
        }

        /*protected override IEnumerable<KeyValuePair<Type, Type>> GetTypesForRegistration()
        {
            return base.GetTypesForRegistration().Concat(new Dictionary<Type, Type>
            {
                { typeof(IFilterProvider)},
            });
        }*/

        private static bool ShouldUsePersistentReadLayer()
        {
            return bool.Parse(WebConfigurationManager.AppSettings["ShouldUsePersistentReadLayer"]);
        }
    }
}