using System.Text;
using WarpToolkit.Windows.Bluetooth;

namespace ObdAdapterProbe;

internal sealed class ElmIdentityProbe
{
    private readonly StringBuilder _response = new();

    internal string? Append(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            if (value == '>')
            {
                string response = _response.ToString().Trim();
                if (!response.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(line => line.StartsWith("ELM327", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("STN", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("OBDLink", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new FormatException($"ATI did not return a recognizable ELM/STN identity: '{response}'.");
                }

                return response;
            }

            if (value > 127 || (value < 32 && value is not (9 or 10 or 13)))
            {
                throw new FormatException("Non-ASCII response; ELM compatibility is not established.");
            }

            if (_response.Length >= 16384)
            {
                throw new FormatException("Identity response exceeded 16 KiB.");
            }

            _response.Append((char)value);
        }

        return null;
    }

    internal static async Task<string> RunAsync(IBluetoothTransport transport, TextWriter log,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        ElmIdentityProbe parser = new();
        await using IAsyncEnumerator<ReadOnlyMemory<byte>> receiver =
            transport.ReceiveAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        Task<bool> pending = receiver.MoveNextAsync().AsTask();
        try
        {
            await log.WriteLineAsync("TX ASCII: ATI\\r");
            await transport.SendAsync("ATI\r"u8.ToArray(), timeout.Token);
            while (await pending)
            {
                ReadOnlyMemory<byte> bytes = receiver.Current;
                await log.WriteLineAsync($"RX HEX: {Convert.ToHexString(bytes.Span)}");
                string? identity = parser.Append(bytes.Span);
                if (identity is not null)
                {
                    return identity;
                }

                pending = receiver.MoveNextAsync().AsTask();
            }

            throw new InvalidOperationException("Transport closed before an ELM identification response.");
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("No complete ATI response within five seconds; protocol compatibility is unverified.", e);
        }
        finally
        {
            timeout.Cancel();
            // Finish an outstanding MoveNext before disposing the async enumerator.
            try
            {
                await pending;
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
            }
        }
    }
}
