﻿namespace AggregateMetrics.Tests.Two
{
    using Microsoft.ApplicationInsights;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationInsights.Extensibility.AggregateMetrics.Two;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.DataContracts;
    using System.Threading;
    using System;
    [TestClass]
    public class TelemetryClientExtensisonsTests
    {
        [TestMethod]
        public void SimpleCounterUsageExample()
        {
            TelemetryConfiguration configuraiton = new TelemetryConfiguration();

            TelemetryClient client = new TelemetryClient(configuraiton);
            client.Context.Properties["a"] = "b";

            var simpleCounter = client.Counter("test");
            var counters = configuraiton.GetCounters();

            Assert.AreEqual(1, counters.Count);

            for (int i = 0; i < 10; i++)
            {
                simpleCounter.Increment();
            }

            MetricTelemetry metric = counters[0].GetValueAndReset();
            Assert.AreEqual(10, metric.Value);
            Assert.AreEqual(null, metric.Count);
            Assert.AreEqual("test", metric.Name);
            Assert.AreEqual("b", metric.Context.Properties["a"]);
        }

        [TestMethod]
        public void SimpleGaugeUsageExample()
        {
            TelemetryConfiguration configuraiton = new TelemetryConfiguration();

            TelemetryClient client = new TelemetryClient(configuraiton);
            client.Context.Properties["a"] = "b";

            client.Gauge("test", () => { return 10; });
            var counters = configuraiton.GetCounters();

            Assert.AreEqual(1, counters.Count);

            MetricTelemetry metric = counters[0].GetValueAndReset();
            Assert.AreEqual(10, metric.Value);
            Assert.AreEqual(null, metric.Count);
            Assert.AreEqual("test", metric.Name);
            Assert.AreEqual("b", metric.Context.Properties["a"]);
        }

        [TestMethod]
        public void SimpleMeterUsageExample()
        {
            TelemetryConfiguration configuraiton = new TelemetryConfiguration();

            TelemetryClient client = new TelemetryClient(configuraiton);
            client.Context.Properties["a"] = "b";

            var simpleMeter = client.Meter("test");
            var counters = configuraiton.GetCounters();

            Assert.AreEqual(1, counters.Count);

            for (int i = 0; i < 10; i++)
            {
                simpleMeter.Mark(2);
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));

            MetricTelemetry metric = counters[0].GetValueAndReset();
            Assert.IsTrue(2 - metric.Value < 1);
            Assert.AreEqual(null, metric.Count);
            Assert.AreEqual("test", metric.Name);
            Assert.AreEqual("b", metric.Context.Properties["a"]);
        }

        [TestMethod]
        public void SimpleHistogramUsageExample()
        {
            TelemetryConfiguration configuraiton = new TelemetryConfiguration();

            TelemetryClient client = new TelemetryClient(configuraiton);
            client.Context.Properties["a"] = "b";

            var simpleMeter = client.Histogram("test");
            var counters = configuraiton.GetCounters();

            Assert.AreEqual(1, counters.Count);

            for (int i = 0; i < 10; i++)
            {
                simpleMeter.Update(i);
            }

            MetricTelemetry metric = counters[0].GetValueAndReset();
            Assert.AreEqual(9 * (9 + 1) / 2 / 10.0, metric.Value);
            Assert.AreEqual(10, metric.Count);
            Assert.AreEqual(0, metric.Min);
            Assert.AreEqual(9, metric.Max);
            Assert.AreEqual("test", metric.Name);
            Assert.AreEqual("b", metric.Context.Properties["a"]);
        }

        [TestMethod]
        public void HistogramPercentilesExample()
        {
            TelemetryConfiguration configuration = new TelemetryConfiguration();

            TelemetryClient client = new TelemetryClient(configuration);

            var histogramWithPercentiles = client.Histogram("test", HistogramAggregations.Percentiles);

            for (int i = 1; i <= 1000; i++)
            {
                histogramWithPercentiles.Update(i);
            }

            var counters = configuration.GetCounters();

            MetricTelemetry metric = counters[0].GetValueAndReset();
            Assert.AreEqual(5, metric.Properties.Count);
            Assert.AreEqual(true, metric.Properties["p50"].Equals("500"));
            Assert.AreEqual(true, metric.Properties["p75"].Equals("750"));
            Assert.AreEqual(true, metric.Properties["p90"].Equals("900"));
            Assert.AreEqual(true, metric.Properties["p95"].Equals("950"));
            Assert.AreEqual(true, metric.Properties["p99"].Equals("990"));
        }

        [TestMethod]
        public void CounterWillCopyTelemetryContextFromTelemetryClient()
        {
            TelemetryConfiguration configuraiton = new TelemetryConfiguration();

            TelemetryClient client = new TelemetryClient(configuraiton);
            client.Context.Properties["a"] = "b";
            client.Context.Component.Version = "10";
            client.Context.InstrumentationKey = "ikey";
            var simpleCounter = client.Counter("test");
            var counters = configuraiton.GetCounters();
            Assert.AreEqual(1, counters.Count);
            MetricTelemetry metric = counters[0].GetValueAndReset();

            client.Context.Device.Id = "device.id";

            // validate that copy was made at the moment of creation
            Assert.AreNotEqual(client.Context, metric.Context);
            Assert.AreNotEqual("device.id", metric.Context.Device.Id);

            Assert.AreEqual("b", metric.Context.Properties["a"]);
            Assert.AreEqual("10", metric.Context.Component.Version);
            Assert.AreEqual("ikey", metric.Context.InstrumentationKey);
        }
    }
}