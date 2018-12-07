﻿namespace Microsoft.ApplicationInsights.Wcf
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Wcf.Implementation;
    using Microsoft.Diagnostics.Instrumentation.Extensions.Intercept;

    /// <summary>
    /// Provides telemetry for web service calls done through the
    /// WCF client-side stack.
    /// </summary>
    /// <remarks>
    /// Only works if the Application Insights Profiler is available.
    /// If this is not the case, you can manually add the necessary
    /// behavior through configuration to your client endpoints.
    /// See <c href="ClientTelemetryExtensionElement">ClientTelemetryExtensionElement</c>
    /// for details.
    /// </remarks>
    public sealed class WcfDependencyTrackingTelemetryModule : ITelemetryModule
    {
        private readonly object lockObject = new object();
        private ProfilerWcfClientProcessing wcfClientProcessing = null;
        private bool initialized = false;

        /// <summary>
        /// Gets or sets the name of the HTTP header to get root operation Id from.
        /// </summary>
        public string RootOperationIdHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the name of the HTTP header to get parent operation Id from.
        /// </summary>
        public string ParentOperationIdHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the name of the SOAP header to get root operation Id from.
        /// </summary>
        public string SoapRootOperationIdHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the name of the SOAP header to get parent operation Id from.
        /// </summary>
        public string SoapParentOperationIdHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the XML Namespace for the root/parent operation ID SOAP headers.
        /// </summary>
        public string SoapHeaderNamespace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable runtime instrumentation.
        /// </summary>
        public bool DisableRuntimeInstrumentation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether channel events (such as channel open) should be emitted as dependencies.
        /// </summary>
        public bool IgnoreChannelEvents { get; set; }

        /// <summary>
        /// Gets the Telemetry Client based on configuration we were initialized with.
        /// </summary>
        internal TelemetryClient TelemetryClient { get; private set; }

        /// <summary>
        /// Initializes this telemetry module.
        /// </summary>
        /// <param name="configuration">Application Insights configuration.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!this.initialized)
            {
                lock (this.lockObject)
                {
                    if (!this.initialized)
                    {
                        try
                        {
                            this.DoInitialization(configuration);
                        }
                        catch (Exception ex)
                        {
                            WcfEventSource.Log.InitializationFailure(ex.ToString());
                        }

                        this.initialized = true;
                    }
                }
            }
        }

        private void DoInitialization(TelemetryConfiguration configuration)
        {
            this.TelemetryClient = new TelemetryClient(configuration);
            if (string.IsNullOrEmpty(this.RootOperationIdHeaderName))
            {
                this.RootOperationIdHeaderName = CorrelationHeaders.HttpStandardRootIdHeader;
            }

            if (string.IsNullOrEmpty(this.ParentOperationIdHeaderName))
            {
                this.ParentOperationIdHeaderName = CorrelationHeaders.HttpStandardParentIdHeader;
            }

            if (this.SoapHeaderNamespace == null)
            {
                this.SoapHeaderNamespace = CorrelationHeaders.SoapStandardNamespace;
            }

            if (string.IsNullOrEmpty(this.SoapParentOperationIdHeaderName))
            {
                this.SoapParentOperationIdHeaderName = CorrelationHeaders.SoapStandardParentIdHeader;
            }

            if (string.IsNullOrEmpty(this.SoapRootOperationIdHeaderName))
            {
                this.SoapRootOperationIdHeaderName = CorrelationHeaders.SoapStandardRootIdHeader;
            }

            if (Decorator.IsHostEnabled())
            {
                WcfClientEventSource.Log.ClientDependencyTrackingInfo("Profiler is attached");
                WcfClientEventSource.Log.ClientDependencyTrackingInfo("Agent version: " + Decorator.GetAgentVersion());
                if (!this.DisableRuntimeInstrumentation)
                {
                    this.wcfClientProcessing = new ProfilerWcfClientProcessing(this);
                    this.DecorateProfilerForWcfClientProcessing();
                }
                else
                {
                    WcfClientEventSource.Log.ClientDependencyTrackingInfo("Runtime Instrumentation is disabled.");
                }
            }
        }

        private void DecorateProfilerForWcfClientProcessing()
        {
            const string Assembly = "System.ServiceModel";
            const string Module = "System.ServiceModel.dll";
            const string ClassName = "System.ServiceModel.ChannelFactory";

            // void InitializeEndpoint(ServiceEndpoint endpoint)
            Functions.Decorate(
                Assembly,
                Module,
                ClassName + ".InitializeEndpoint",
                this.wcfClientProcessing.OnStartInitializeEndpoint1,
                this.wcfClientProcessing.OnEndInitializeEndpoint1,
                null,
                isStatic: false,
                isSafe: true);

            // void InitializeEndpoint(Binding binding, EndpointAddress address)
            // void InitializeEndpoint(string configurationName, EndpointAddress address)
            Functions.Decorate(
                Assembly,
                Module,
                ClassName + ".InitializeEndpoint",
                this.wcfClientProcessing.OnStartInitializeEndpoint2,
                this.wcfClientProcessing.OnEndInitializeEndpoint2,
                null,
                isStatic: false,
                isSafe: true);

            // void InitializeEndpoint(string configurationName, EndpointAddress address, Configuration configuration)
            Functions.Decorate(
                Assembly,
                Module,
                ClassName + ".InitializeEndpoint",
                this.wcfClientProcessing.OnStartInitializeEndpoint3,
                this.wcfClientProcessing.OnEndInitializeEndpoint3,
                null,
                isStatic: false,
                isSafe: true);
        }
    }
}
