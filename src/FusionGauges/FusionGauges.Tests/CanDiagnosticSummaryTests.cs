using WarpToolkit.Windows.Bluetooth;
using WinFormsFusionGauges.WinFormsApp;

namespace FusionGauges.Tests;

/// <summary>Checks diagnostic outcomes independently of terminal text retention and acquisition.</summary>
public sealed class CanDiagnosticSummaryTests
{
    [Fact]
    public void SummaryDistinguishesAdapterAcknowledgementsVehicleFramesAndMissingSignals()
    {
        CanDiagnosticSummary summary = new();
        Assert.All(summary.Capture(), row => Assert.Equal("Not attempted", row.Result));
        Add(summary, "TX", "ATCSM1");
        Add(summary, "RX", "OK");
        Add(summary, "TX", "STM");
        DateTimeOffset received = DateTimeOffset.Parse("2026-10-25T12:00:00Z");
        summary.Append(new(received, "RX", "105 09 C4 7D 00 00 BF 33 3B"));
        Add(summary, "TX", "[space: stop monitor]");
        Add(summary, "RX", "STOPPED");
        Add(summary, "INFO", "Result: STM: 1 raw frames, 3 signal candidates.");
        CanDiagnosticResult[] rows = summary.Capture();
        CanDiagnosticResult setup = rows.Single(row => row.Command == "ATCSM1");
        Assert.Equal("Accepted (not vehicle data)", setup.Result);
        Assert.Equal(0, setup.Samples);
        CanDiagnosticResult monitor = rows.Single(row => row.Command == "STM");
        Assert.Equal("Data received", monitor.Result);
        Assert.Equal(1, monitor.Samples);
        Assert.Equal("105 09 C4 7D 00 00 BF 33 3B", monitor.Response);
        CanDiagnosticResult rpm = rows.Single(row => row.Reading == "EngineRpm");
        Assert.Equal("2500", rpm.RawValue);
        Assert.Equal("2500", rpm.Value);
        Assert.Equal("rpm", rpm.Unit);
        Assert.Equal(received, rpm.Timestamp);
        Assert.Contains("UNVERIFIED", rpm.Result);
        CanDiagnosticResult torque = rows.Single(row => row.Reading == "EngineTorque");
        Assert.Contains("No data observed", torque.Result);
        Assert.Equal(1, torque.Attempts);
        Assert.Equal("", torque.Value);
        Assert.Equal(0, torque.Samples);
        Assert.DoesNotContain(rows, row => row.Command == "[space: stop monitor]");
    }

    [Fact]
    public void EmptyRejectedAndTruncatedRepliesNeverProduceInventedValues()
    {
        CanDiagnosticSummary summary = new();
        Add(summary, "TX", "STFCP");
        Add(summary, "RX", "?");
        Add(summary, "TX", "ATCF105");
        Add(summary, "RX", "OK");
        Add(summary, "TX", "ATMA");
        Add(summary, "RX", "105 09");
        Add(summary, "INFO", "Result: ATMA: 1 raw frames, 0 signal candidates.");
        CanDiagnosticResult[] rows = summary.Capture();
        Assert.Equal("Rejected / adapter response", rows.Single(row => row.Command == "STFCP").Result);
        CanDiagnosticResult rpm = rows.Single(row => row.Reading == "EngineRpm");
        Assert.Equal("Short payload; not decoded", rpm.Result);
        Assert.Equal("", rpm.RawValue);
        Assert.Equal("", rpm.Value);
        Assert.Equal("Not attempted", rows.Single(row => row.Reading == "EngineTorque").Result);
        Add(summary, "TX", "ATMA");
        Add(summary, "RX", "NO DATA");
        Add(summary, "INFO", "Result: ATMA: 0 raw frames, 0 signal candidates.");
        Assert.Equal("No frame data", summary.Capture().Single(row => row.Command == "ATMA").Result);
        Assert.Equal(2, summary.Capture().Single(row => row.Command == "ATMA").Attempts);
    }

    [Fact]
    public void SummaryKeepsLatestReadingAndCountsAcrossThreeAttemptsAndResetsOnlyForANewInvestigation()
    {
        CanDiagnosticSummary summary = new();
        for (int index = 0; index < 3; index++)
        {
            Add(summary, "TX", "STM");
            Add(summary, "RX", $"105 09 {0xC4 + index:X2}");
            Add(summary, "INFO", "Result: STM: 1 raw frames, 1 signal candidates.");
        }

        CanDiagnosticResult rpm = summary.Capture().Single(row => row.Reading == "EngineRpm");
        Assert.Equal(3, rpm.Attempts);
        Assert.Equal(3, rpm.Samples);
        Assert.Equal("2502", rpm.Value);
        Assert.Equal("105 09 C6", rpm.Response);
        Add(summary, "INFO", "Read-only CAN capture queued; at most 3 attempts, then stop.");
        Assert.All(summary.Capture(), row =>
        {
            Assert.Equal("Not attempted", row.Result);
            Assert.Equal(0, row.Samples);
            Assert.Equal("", row.Response);
        });
    }

    [Fact]
    public void InterruptedCommandIsVisibleWithoutAFalseAcknowledgement()
    {
        CanDiagnosticSummary summary = new();
        Add(summary, "TX", "ATCAF0");
        Add(summary, "INFO", "Capture cancelled or time budget elapsed.");
        Assert.Equal("No response observed / interrupted",
            summary.Capture().Single(row => row.Command == "ATCAF0").Result);
    }

    [Fact]
    public void StopAcknowledgementDoesNotHideAMonitorRejection()
    {
        CanDiagnosticSummary summary = new();
        Add(summary, "TX", "STM");
        Add(summary, "RX", "?");
        Add(summary, "TX", "[space: stop monitor]");
        Add(summary, "RX", "STOPPED");
        Add(summary, "INFO", "Result: STM: 0 raw frames, 0 signal candidates.");
        CanDiagnosticResult monitor = summary.Capture().Single(row => row.Command == "STM");
        Assert.Equal("Rejected / monitor error", monitor.Result);
        Assert.Equal("?", monitor.Response);
        Assert.Equal(0, monitor.Samples);
    }

    private static void Add(CanDiagnosticSummary summary, string direction, string message)
        => summary.Append(new(DateTimeOffset.UtcNow, direction, message));
}
