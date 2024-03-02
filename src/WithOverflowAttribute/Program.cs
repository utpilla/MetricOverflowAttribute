using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

public class Program
{
    private static readonly Meter TestMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private static readonly Histogram<long> TestHistogram = TestMeter.CreateHistogram<long>("HistogramWithOverflow");

    public static void Main()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_EXPERIMENTAL_METRICS_EMIT_OVERFLOW_ATTRIBUTE", "true");

        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("MyCompany.MyProduct.MyLibrary")
            .SetMaxMetricPointsPerMetricStream(5)
            .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
            {
                exporterOptions.Endpoint = new Uri("http://localhost:9090/api/v1/otlp/v1/metrics");
                exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
            })
            .Build();

        // Emit metrics for a minute
        for (int i = 0; i < 60; i++)
        {
            TestHistogram.Record(2, new KeyValuePair<string, object?>("Customer", "A"));
            TestHistogram.Record(3, new KeyValuePair<string, object?>("Customer", "B"));
            TestHistogram.Record(4, new KeyValuePair<string, object?>("Customer", "C"));

            // Emit additional Metric Points after 30 seconds
            if (i >= 30)
            {
                TestHistogram.Record(1, new KeyValuePair<string, object?>("Customer", "D"));
                TestHistogram.Record(5, new KeyValuePair<string, object?>("Customer", "E"));
            }

            Thread.Sleep(1000);
        }

        // Dispose meter provider before the application ends.
        // This will flush the remaining metrics and shutdown the metrics pipeline.
        meterProvider.Dispose();
    }
}