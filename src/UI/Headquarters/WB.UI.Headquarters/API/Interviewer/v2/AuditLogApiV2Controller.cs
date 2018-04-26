﻿using System;
using System.Reflection;
using System.Web.Http;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.InterviewerAuditLog;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Views.InterviewerAuditLog;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.UI.Headquarters.Code;


namespace WB.UI.Headquarters.API.Interviewer.v2
{
    [ApiBasicAuth(new[] { UserRoles.Interviewer })]
    public class AuditLogApiV2Controller : ApiController
    {
        private readonly IPlainStorageAccessor<AuditLogRecord> auditLogStorage;
        private readonly IJsonAllTypesSerializer typesSerializer;
        private readonly IAuditLogTypeResolver auditLogTypeResolver;

        public AuditLogApiV2Controller(IPlainStorageAccessor<AuditLogRecord> auditLogStorage,
            IJsonAllTypesSerializer typesSerializer,
            IAuditLogTypeResolver auditLogTypeResolver)
        {
            this.auditLogStorage = auditLogStorage;
            this.typesSerializer = typesSerializer;
            this.auditLogTypeResolver = auditLogTypeResolver;
        }

        [HttpPost]
        public IHttpActionResult Post(AuditLogEntitiesApiView entities)
        {
            if (entities == null)
                return this.BadRequest("Server cannot accept empty package content.");

            foreach (var auditLogEntity in entities.Entities)
            {
                var payloadType = auditLogTypeResolver.Resolve(auditLogEntity.PayloadType);
                var auditLogRecord = new AuditLogRecord()
                {
                    RecordId = auditLogEntity.Id,
                    ResponsibleId = auditLogEntity.ResponsibleId,
                    ResponsibleName = auditLogEntity.ResponsibleName,
                    Time = auditLogEntity.Time,
                    TimeUtc = auditLogEntity.TimeUtc,
                    Type = auditLogEntity.Type,
                    Payload = typesSerializer.Deserialize<IAuditLogEntity>(auditLogEntity.Payload, payloadType)
                };
                auditLogStorage.Store(auditLogRecord, auditLogRecord.Id);
            }

            return this.Ok();
        }
    }
}
