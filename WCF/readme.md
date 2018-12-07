# Application Insights SDK WCF Telemetry Module

>The Microsoft Application Insights API SDK enables you to instrument your .NET application to track performance and usage. 
> -- <cite>[Getting started with Application Insights](http://azure.microsoft.com/documentation/articles/app-insights-get-started/)</cite> 

This project extends the Application Insights SDK for .NET to provide telemetry for WCF services.
This provides a better telemetry experience than with the SDK for Web Applications:

* Operation names will now include the service operation being invoked
* Support adding telemetry for WCF services exposed over non-HTTP bindings, such as Net.TCP
* Supports tracking service errors through an IErrorHandler extension
* Selective control of which services are monitored
* Now supports WCF REST services using webHttpBinding


Requirements
------------
[Microsoft Application Insights Windows Server](https://www.nuget.org/packages/Microsoft.ApplicationInsights.WindowsServer)

Installation
------------
- Add the [Application Insights SDK Labs](https://www.myget.org/gallery/applicationinsights-sdk-labs) package source to NuGet
- Add the prerelease package `Microsoft.ApplicationInsights.Wcf` to your project.
 - If you're using the Manage NuGet Packages GUI, remember to include prerelease packages.
- Instrument your WCF service using one of the two following methods:
  - Mark your service class with the `[ServiceTelemetry]` attribute
  - Add the `<serviceTelemetry/>` service behavior through configuration to your service. By default, this behavior will be added to the unnamed `<serviceBehavior>` element when the NuGet package is added to the project.
- Add [the Instrumentation Key of your Application Insights resource](https://azure.microsoft.com/documentation/articles/app-insights-create-new-resource/) to ApplicationInsights.config
  - Or you can also provide the instrumentation key [through code or web config](https://azure.microsoft.com/documentation/articles/app-insights-api-custom-events-metrics/#ikey).
- That's it!

Using the command line package manager? This is what you need.
```
> Install-Package "Microsoft.ApplicationInsights.Wcf" -Source "https://www.myget.org/F/applicationinsights-sdk-labs/" -Pre
```


Controlling Operation Telemetry
-------------------------------
By default, requests arriving at any operation in the service contract
will trigger request telemetry to be sent. However, it is sometimes
useful to control the volume of events being sent to Application
Insights by only instrumenting some operations.

You can do this by marking any operations you want to explicitly
send request telemetry for with the `[OperationTelemetry]` attribute,
either on your service contract or your service implementation.

When you do this, any requests arriving to an operation
that does __not__ have an `[OperationTelemetry]` attribute
will not generate a request telemetry event.


Obtaining the current request context
-------------------------------------
If you need to set a value on the `RequestTelemetry` event in your application code,
you can get access to it through the `GetRequestTelemetry()` extension method
on the current `OperationContext`, like this:

```C#
using Microsoft.ApplicationInsights.Wcf;
...
var request = OperationContext.Current.GetRequestTelemetry();
```


Current Limitations
---------------------
- Operation duration will not track how long the call was throttled by WCF due to the `<serviceThrottling>` configuration.


Client Dependency Telemetry Tracking
---------------------
With version 0.27.0, a new experimental feature is introduced to help emit `DependencyTelemetry` events
when the client-side channel stack in WCF is used to call a Web Service. This feature works
by hooking into the channel stack and emiting telemetry for the channel open and send operations.

This feature is disabled by default, but can be enabled in two ways:

The first option is to create a new Endpoint Behavior and add the `<clientTelemetry/>` element to it:

```XML
<behaviors>
  <endpointBehaviors>
    <behavior name="clientEndpoints">
      <clientTelemetry />
    </behavior>
  </endpointBehaviors>
</behaviors>
```

Then register this behavior in your client endpoint:

```XML
<client>
  <endpoint address="..."
            ...
            behaviorConfiguration="clientEndpoints" />
</client>
```

The second option will take advantage of the Application Insights Profiler if it's available
to automatically instrument all client-side proxies in your application. You can
enable this by editing your `ApplicationInsights.config` file and registering the corresponding
`TelemetryModule`:

```XML
<TelemetryModules>
    ...
    <Add Type="Microsoft.ApplicationInsights.Wcf.WcfDependencyTrackingTelemetryModule, Microsoft.AI.Wcf"/>
</TelemetryModules>
``` 

__Limitations__
* Callback contracts (i.e. duplex) are not supported
* When using asynchronous calls to your WCF service, dependency telemetry events might not get correctly
assiociated with the request on an ASP.NET application due to the `HttpContext` not being correctly propagated.
* When using HTTP-based bindings, you will see multiple dependency telemetry events being generated for the same
request if the Application Insights `DependencyTrackingModule` is enabled. This is because the module will emit a separate
event for the HTTP request itself.