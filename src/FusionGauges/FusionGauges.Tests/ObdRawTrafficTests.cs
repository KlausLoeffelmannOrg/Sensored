using System.Text.Json;
using Sensored;
using WarpToolkit.Windows.Bluetooth;

namespace FusionGauges.Tests;

/// <summary>Checks raw OBD capture independently of UI selection and physical adapters.</summary>
public sealed class ObdRawTrafficTests
{
    [Fact]
    public async Task RecordingsKeepExactMessagesOutsideTheBoundedDisplayTailAndCanRestart()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-raw-obd-{Guid.NewGuid():N}");
        string first = Path.Combine(folder, "first.jsonl");
        string second = Path.Combine(folder, "second.jsonl");
        try
        {
            await using ObdRawTrafficCollector collector = new();
            collector.Publish(new(DateTimeOffset.UtcNow, "RX", "before recording"));
            await collector.StartRecordingAsync(first);
            for (int index = 0; index < 600; index++)
            {
                collector.Publish(new(DateTimeOffset.UnixEpoch.AddMilliseconds(index), "RX", $"frame {index}\r\n>"));
            }

            await collector.StopRecordingAsync();
            string[] lines = await File.ReadAllLinesAsync(first, TestContext.Current.CancellationToken);
            Assert.Equal(600, lines.Length);
            Assert.Equal("frame 0\r\n>", JsonSerializer.Deserialize<ObdDiagnosticEntry>(lines[0])!.Message);
            Assert.Equal(DateTimeOffset.UnixEpoch,
                JsonSerializer.Deserialize<ObdDiagnosticEntry>(lines[0])!.Timestamp);
            Assert.Equal("frame 599\r\n>", JsonSerializer.Deserialize<ObdDiagnosticEntry>(lines[^1])!.Message);
            ObdRawTrafficSnapshot snapshot = collector.ReadSnapshot();
            Assert.Equal(500, snapshot.Entries.Length);
            Assert.Equal("frame 100\r\n>", snapshot.Entries[0].Message);
            Assert.False(snapshot.Recording);
            collector.Publish(new(DateTimeOffset.UtcNow, "TX", "between recordings"));
            await collector.StartRecordingAsync(second);
            collector.Publish(new(DateTimeOffset.UtcNow, "TX", "010D"));
            await collector.StopRecordingAsync();
            Assert.Equal("010D", JsonSerializer.Deserialize<ObdDiagnosticEntry>(
                Assert.Single(await File.ReadAllLinesAsync(second, TestContext.Current.CancellationToken)))!.Message);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder);
            }
        }
    }

    [Fact]
    public async Task DeadlineClosesAndFlushesAnIdleRecordingWithoutAnyUiOrFurtherMessages()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-raw-obd-{Guid.NewGuid():N}.jsonl");
        try
        {
            await using ObdRawTrafficCollector collector = new(TimeSpan.FromMilliseconds(100));
            await collector.StartRecordingAsync(path);
            collector.Publish(new(DateTimeOffset.UtcNow, "RX", "within limit"));
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
            while (collector.ReadSnapshot().Recording)
            {
                await Task.Delay(20, timeout.Token);
            }

            collector.Publish(new(DateTimeOffset.UtcNow, "RX", "after limit"));
            await collector.StopRecordingAsync();
            Assert.Equal("within limit", JsonSerializer.Deserialize<ObdDiagnosticEntry>(
                Assert.Single(await File.ReadAllLinesAsync(path, timeout.Token)))!.Message);
            Assert.Null(collector.Fault);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task FileCreationFailuresAreTerminalVisibleAndDoNotOverwriteExistingData()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-raw-obd-{Guid.NewGuid():N}.jsonl");
        try
        {
            await File.WriteAllTextAsync(path, "existing data", TestContext.Current.CancellationToken);
            ObdRawTrafficCollector collector = new();
            await Assert.ThrowsAsync<IOException>(() => collector.StartRecordingAsync(path));
            await Assert.ThrowsAsync<IOException>(() => collector.DisposeAsync().AsTask());
            Assert.NotNull(collector.Fault);
            Assert.Equal("existing data", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
