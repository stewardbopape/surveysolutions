﻿using Ninject.Modules;

using WB.Core.BoundedContexts.Tester.Implementation.Services;
using WB.Core.BoundedContexts.Tester.Services;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.UI.Shared.Enumerator.CustomServices;

namespace WB.UI.Tester.Ninject
{
    public class TesterUIModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IViewModelNavigationService>().To<ViewModelNavigationService>();
        }
    }
}