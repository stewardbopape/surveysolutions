using System;
using System.Diagnostics;
using System.Reflection;
using WB.Core.Infrastructure.Versions;

namespace WB.UI.Designer.CommonWeb
{
    public class ProductVersion : IProductVersion
    {
        private Assembly assembly;

        public ProductVersion()
        {
            this.assembly = typeof(Startup).Assembly;
        }

        public override string ToString() => FileVersionInfo.GetVersionInfo(this.assembly.Location).ProductVersion;
        public Version GetVersion() => new Version(this.ToString().Split(' ')[0]);
        public int GetBildNumber() => FileVersionInfo.GetVersionInfo(this.assembly.Location).FilePrivatePart;
    }
}
