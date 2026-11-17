namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies recording output cleanup after a failed flush.
/// </summary>
public sealed class SemicolonCsvRecordingWriterTests
{
    [Fact]
    public async Task DisposeClosesTheStreamWhenFlushingFails()
    {
        FailingWriteStream stream = new();
        SemicolonCsvRecordingWriter writer = new(stream);
        await writer.WriteHeaderAsync(TestContext.Current.CancellationToken);

        IOException exception = await Assert.ThrowsAsync<IOException>(
            () => writer.DisposeAsync().AsTask());

        Assert.Equal("Simulated write failure.", exception.Message);
        Assert.False(stream.CanWrite);
    }

    /// <summary>
    ///  Simulates a storage failure when buffered text is flushed to the output.
    /// </summary>
    private sealed class FailingWriteStream : MemoryStream
    {
        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException(new IOException("Simulated write failure."));
    }
}
