﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Infrastructure.Native.Storage
{
    public static class EventSerializerSettings
    {
        public static readonly JsonSerializerSettings BackwardCompatibleJsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new JsonConverter[] { new StringEnumConverter(), IdentityJsonConverter.Instance, RosterVectorConvertor.Instance },
            Binder = new OldToNewAssemblyRedirectSerializationBinder()
        };
    }
}