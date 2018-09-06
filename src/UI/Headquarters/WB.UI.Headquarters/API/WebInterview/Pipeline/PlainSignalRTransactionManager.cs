﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace WB.UI.Headquarters.API.WebInterview.Pipeline
{
    public class PlainSignalRTransactionManager : HubPipelineModule
    {
        public override Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(
            Func<IHubIncomingInvokerContext, Task<object>> invoke)
        {

            return async context =>
            {
                try
                {
                    // This is responsible for invoking every server-side Hub method in your SignalR app.
                    return await invoke(context);
                }
                catch (Exception e)
                {
                    // If a Hub method throws, have it return the error message instead.
                    return e.Message;
                }
            };

            /*return base.BuildIncoming(context =>
            {
                scope = new NinjectAmbientScope();
                return base.BuildIncoming(invoke);

                /*using (var scope = new NinjectAmbientScope())
                {
                    var unitOfWork = Get<IUnitOfWork>();
                    
                    try
                    {
                        var result = (invoke(context)).Result;
                        unitOfWork.AcceptChanges();
                        return Task.FromResult((object)result);
                    }
                    catch (Exception)
                    {
                        unitOfWork.Dispose();
                        throw;
                    }
                }#1#
            });*/
        }

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            // TODO CHECK HOW TO HANDLE TRANSACTIONS HERE
            return base.OnBeforeIncoming(context);
        }

        protected override object OnAfterIncoming(object result, IHubIncomingInvokerContext context)
        {
            return base.OnAfterIncoming(result, context);
        }

        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            base.OnIncomingError(exceptionContext, invokerContext);
        }
    }
}
