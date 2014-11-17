using WB.Core.GenericSubdomains.Utils.Rest;
using WB.Core.SharedKernel.Utils.Serialization;

namespace WB.Core.GenericSubdomains.Rest.Android
{
    internal class AndroidRestServiceWrapperFactory : IRestServiceWrapperFactory
    {
        private readonly IJsonUtils jsonUtils;

        public AndroidRestServiceWrapperFactory(IJsonUtils jsonUtils)
        {
            this.jsonUtils = jsonUtils;
        }

        public IRestServiceWrapper CreateRestServiceWrapper(string baseAddress, bool acceptUnsignedCertificate = true)
        {
            return new AndroidRestServiceWrapper(baseAddress, this.jsonUtils, acceptUnsignedCertificate);
        }
    }
}