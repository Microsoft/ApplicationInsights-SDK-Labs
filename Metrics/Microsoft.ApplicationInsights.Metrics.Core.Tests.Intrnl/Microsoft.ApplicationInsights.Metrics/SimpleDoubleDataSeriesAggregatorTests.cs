﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

using Microsoft.ApplicationInsights.Metrics.TestUtil;
using System.Globalization;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class SimpleDoubleDataSeriesAggregatorTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SimpleDoubleDataSeriesAggregator(configuration: null, dataSeries: null, consumerKind: MetricConsumerKind.Custom));

            Assert.ThrowsException<ArgumentException>(() => new SimpleDoubleDataSeriesAggregator(
                                                                           new NaiveDistinctCountMetricSeriesConfiguration(),
                                                                           dataSeries: null,
                                                                           consumerKind: MetricConsumerKind.Custom));

            Assert.ThrowsException<ArgumentException>(() => new SimpleDoubleDataSeriesAggregator(
                                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                                            dataSeries: null,
                                                                            consumerKind: MetricConsumerKind.Custom));

            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);
                Assert.IsNotNull(aggregator);
            }
            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);
                Assert.IsNotNull(aggregator);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueDouble()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                // Empty aggregator:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Zero value:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(0);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Non zero value:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(-42);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: -42.0, max: -42.0, min: -42.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Two values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(-42);
                aggregator.TrackValue(18);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -24.0, max: 18.0, min: -42.0, stdDev: 30.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // 3 values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);
                aggregator.TrackValue(1800000);
                aggregator.TrackValue(0);
                aggregator.TrackValue(-4200000);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: -2400000.0, max: 1800000.0, min: -4200000.0, stdDev: 2513961.018, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // NaNs:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);
                aggregator.TrackValue(Double.NaN);
                aggregator.TrackValue(1);
                aggregator.TrackValue(Double.NaN);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 1, min: 1, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Infinity:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(1);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 1, min: 1, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(0.5);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 1.5, max: 1, min: 0.5, stdDev: 0.25, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.PositiveInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: Double.MaxValue, max: Double.MaxValue, min: 0.5, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Int32.MinValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: Double.MaxValue, max: Double.MaxValue, min: Int32.MinValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.PositiveInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: Double.MaxValue, max: Double.MaxValue, min: Int32.MinValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.NegativeInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 0.0, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(11);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 0.0, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Very large numbers:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Math.Exp(300));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: Math.Exp(300), max: Math.Exp(300), min: Math.Exp(300), stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-2 * Math.Exp(300));
                double minus2exp200 = -2 * Math.Exp(300);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -Math.Exp(300), max: Math.Exp(300), min: minus2exp200, stdDev: 2.91363959286188000E+130, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Math.Exp(300));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 0, max: Math.Exp(300), min: minus2exp200, stdDev: 2.74700575206167000E+130, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Math.Exp(700));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: Math.Exp(700), max: Math.Exp(700), min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: Double.MaxValue, max: Double.MaxValue, min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: Double.MaxValue, max: Double.MaxValue, min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(11);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: Double.MaxValue, max: Double.MaxValue, min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-Double.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: Double.MaxValue, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-Double.PositiveInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 0, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Large number of small values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                for (int i = 0; i < 100000; i++)
                {
                    for (double v = 0; v <= 1.0 || Math.Abs(1.0 - v) < Utils.MaxAllowedPrecisionError; v += 0.01)
                    {
                        aggregator.TrackValue(v);
                    }
                }

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10100000, sum: 5050000, max: 1, min: 0, stdDev: 0.29154759474226500, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Large number of large values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                for (int i = 0; i < 100000; i++)
                {
                    for (double v = 0; v <= 300000.0 || Math.Abs(300000.0 - v) < Utils.MaxAllowedPrecisionError; v += 3000)
                    {
                        aggregator.TrackValue(v);
                    }
                }

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10100000, sum: 1515000000000, max: 300000, min: 0, stdDev: 87464.2784226795, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            aggregator.TrackValue(null);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Boolean) true) );

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (SByte) (0-1));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: -1, max: -1, min: -1, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Byte) 2);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 1, max: 2, min: -1, stdDev: 1.5, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Int16) (0-3));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: -2, max: 2, min: -3, stdDev: 2.05480466765633, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (UInt16) 4);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 2, max: 4, min: -3, stdDev: 2.69258240356725, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Int32) (0-5));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: -3, max: 4, min: -5, stdDev: 3.26190128606002, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (UInt32) 6);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 3, max: 6, min: -5, stdDev: 3.86221007541882, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Int64) (0-7));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: -4, max: 6, min: -7, stdDev: 4.43547848464572, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (UInt64) 8);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 4, max: 8, min: -7, stdDev: 5.02493781056044, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (IntPtr) 0xFF) );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (UIntPtr) 0xFF) );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Char) 'x') );

            aggregator.TrackValue((object) (Single) (0f-9.0f));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: -5, max: 8, min: -9, stdDev: 5.59982363037962000, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Double) 10.0);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 10, sum: 5, max: 10, min: -9, stdDev: 6.18465843842649000, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("-11");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: -6, max: 10, min: -11, stdDev: 6.76036088821026, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("12.00");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 12, sum: 6, max: 12, min: -11, stdDev: 7.34279692397023, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("-1.300E+01");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 13, sum: -7, max: 12, min: -13, stdDev: 7.91896831484996, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("  +14. ");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 7, max: 14, min: -13, stdDev: 8.5, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("fifteen") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("foo-bar") );

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 7, max: 14, min: -13, stdDev: 8.5, timestamp: default(DateTimeOffset), periodMs: periodString);
           
        }

        private static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, string periodMs)
        {
            CommonSimpleDataSeriesAggregatorTests.ValidateNumericAggregateValues(aggregate, name, count, sum, max, min, stdDev, timestamp, periodMs);
        }

        /// <summary />
        [TestMethod]
        public void CreateAggregateUnsafe()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    metric.GetConfiguration(),
                                                    metric,
                                                    MetricConsumerKind.Custom);

            CommonSimpleDataSeriesAggregatorTests.CreateAggregateUnsafe(aggregator, metric);
        }

        /// <summary />
        [TestMethod]
        public void TryRecycle()
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            var measurementAggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            var counterAggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);


            CommonSimpleDataSeriesAggregatorTests.TryRecycle(measurementAggregator, counterAggregator);
        }

        /// <summary />
        [TestMethod]
        public void GetDataSeries()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregatorForConcreteSeries = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: metric,
                                                    consumerKind: MetricConsumerKind.Custom);

            var aggregatorForNullSeries = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

            CommonSimpleDataSeriesAggregatorTests.GetDataSeries(aggregatorForConcreteSeries, aggregatorForNullSeries, metric);
        }

        /// <summary />
        [TestMethod]
        public void Reset()
        {
            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                CommonSimpleDataSeriesAggregatorTests.Reset(aggregator);
            }
            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                CommonSimpleDataSeriesAggregatorTests.Reset(aggregator);
            }

        }

        /// <summary />
        [TestMethod]
        public void CompleteAggregation()
        {
            var aggregationManager = new MetricAggregationManager();

            var mesurementConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            var measurementMetric = new MetricSeries(aggregationManager, "Cows Sold", mesurementConfig);

            var measurementAggregator = new SimpleDoubleDataSeriesAggregator(
                                                    measurementMetric.GetConfiguration(),
                                                    measurementMetric,
                                                    MetricConsumerKind.Custom);

            var counterConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false);
            var counterMetric = new MetricSeries(aggregationManager, "Cows Sold", counterConfig);

            var counterAggregator = new SimpleDoubleDataSeriesAggregator(
                                                    counterMetric.GetConfiguration(),
                                                    counterMetric,
                                                    MetricConsumerKind.Custom);

            CommonSimpleDataSeriesAggregatorTests.CompleteAggregation(measurementAggregator, counterAggregator);
        }
    }
}