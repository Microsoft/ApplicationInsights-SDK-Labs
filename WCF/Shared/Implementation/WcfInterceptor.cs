﻿namespace Microsoft.ApplicationInsights.Wcf.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationInsights.Extensibility;
    using TelemetryModules = Microsoft.ApplicationInsights.Extensibility.Implementation.TelemetryModules;

    internal class WcfInterceptor : IDispatchMessageInspector, IErrorHandler
    {
        private TelemetryConfiguration configuration;
        private ContractFilter contractFilter;

        public WcfInterceptor(TelemetryConfiguration configuration)
            : this(configuration, null)
        {
        }

        public WcfInterceptor(TelemetryConfiguration configuration, ContractFilter filter)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
 
            this.configuration = configuration;
            this.contractFilter = filter;
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            IOperationContext context = WcfOperationContext.Current;
            if (context != null)
            {
                WcfOperationContext.StoreThreadContext(context);

                if (!this.LogTelemetryFor(context))
                {
                    WcfEventSource.Log.OperationIgnored(context.ContractName, context.ContractNamespace, context.OperationName);
                    return null;
                }

                foreach (var mod in this.GetModules())
                {
                    Executor.ExceptionSafe(
                        mod.GetType().Name,
                        "OnBeginRequest",
                        mod.OnBeginRequest,
                        context);
                }

                foreach (var mod in this.GetModules())
                {
                    var tracer = mod as IWcfMessageTrace;
                    if (tracer != null)
                    {
                        Executor.ExceptionSafe(
                            mod.GetType().Name,
                            "OnTraceRequest",
                            tracer.OnTraceRequest,
                            context,
                            ref request);
                    }
                }
            }
            else
            {
                WcfEventSource.Log.NoOperationContextFound();
            }

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var context = WcfOperationContext.Current;
            if (context != null)
            {
                WcfOperationContext.ClearThreadContext();

                // Do not run OnEndRequest for stuff we're not interested in!
                if (!this.LogTelemetryFor(context))
                {
                    return;
                }

                foreach (var mod in this.GetModules())
                {
                    var tracer = mod as IWcfMessageTrace;
                    if (tracer != null)
                    {
                        Executor.ExceptionSafe(
                            mod.GetType().Name,
                            "OnTraceResponse",
                            tracer.OnTraceResponse,
                            context,
                            ref reply);
                    }
                }

                foreach (var mod in this.GetModules())
                {
                    Executor.ExceptionSafe(
                        mod.GetType().Name,
                        "OnEndRequest",
                        mod.OnEndRequest,
                        context,
                        reply);
                }
            }
            else
            {
                WcfEventSource.Log.NoOperationContextFound();
            }
        }

        public bool HandleError(Exception error)
        {
            return false;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            var context = WcfOperationContext.Current;
            if (context != null)
            {
                foreach (var mod in this.GetModules())
                {
                    Executor.ExceptionSafe(
                        mod.GetType().Name,
                        "OnError",
                        mod.OnError,
                        context,
                        error);
                }
            }
            else
            {
                WcfEventSource.Log.NoOperationContextFound();
            }
        }

        private IEnumerable<IWcfTelemetryModule> GetModules()
        {
            return TelemetryModules.Instance.Modules.OfType<IWcfTelemetryModule>();
        }

        private bool LogTelemetryFor(IOperationContext context)
        {
            if (this.contractFilter == null)
            {
                return true;
            }

            return this.contractFilter.ShouldProcess(
                context.ContractName,
                context.ContractNamespace,
                context.OperationName);
        }
    }
}
