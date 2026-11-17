using System.Globalization;
using WarpToolkit.Windows.Bluetooth;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Retains one investigation's command outcomes and latest signal readings.</summary>
/// <remarks>Only raw RX frames are decoded; adapter acknowledgements never count as vehicle data.</remarks>
internal sealed class CanDiagnosticSummary
{
    private readonly MercedesEgs53Module _module = new();
    private readonly Dictionary<string, CanDiagnosticResult> _commands = [];
    private readonly Dictionary<string, CanDiagnosticResult> _signals = [];
    private string? _command;
    private uint? _filter;
    private bool _monitoring;
    private readonly HashSet<uint> _observedIds = [];

    /// <summary>Creates an empty summary with every configured signal visible as not attempted.</summary>
    internal CanDiagnosticSummary() => Reset();

    /// <summary>Captures detached display rows, independent of the rolling terminal's line limit.</summary>
    internal CanDiagnosticResult[] Capture()
        => [.. _commands.Values, .. _signals.Values];

    /// <summary>Updates results on the UI thread from original adapter events.</summary>
    internal void Append(ObdDiagnosticEntry entry)
    {
        if (entry.Direction == "INFO"
            && entry.Message.StartsWith("Read-only CAN capture queued;", StringComparison.Ordinal))
        {
            Reset();

            return;
        }

        if (entry.Direction == "TX")
        {
            if (entry.Message == "[space: stop monitor]")
            {
                return;
            }

            FinishCommand();
            _command = entry.Message;
            _monitoring = _command is "STM" or "ATMA";
            if (_command.StartsWith("ATCF", StringComparison.Ordinal)
                && _command.Length == 7
                && uint.TryParse(_command.AsSpan(4), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out uint id))
            {
                _filter = id;
            }

            if (_commands.TryGetValue(_command, out CanDiagnosticResult? previous))
            {
                _commands[_command] = previous with
                {
                    Attempts = previous.Attempts + 1,
                    Result = "Waiting for response",
                    Response = "",
                    Timestamp = null
                };
            }
            else
            {
                _commands.Add(_command, new(_command, "Adapter command", "Waiting for response",
                    1, 0, "", "", "", "", null));
            }

            if (_monitoring)
            {
                foreach (CanSignalDefinition signal in MonitoredSignals())
                {
                    _observedIds.Add(signal.CanId);
                    CanDiagnosticResult row = _signals[signal.Name];
                    _signals[signal.Name] = row with { Attempts = row.Attempts + 1 };
                }
            }

            return;
        }

        if (entry.Direction == "INFO"
            && (entry.Message.StartsWith("Result: STM:", StringComparison.Ordinal)
                || entry.Message.StartsWith("Result: ATMA:", StringComparison.Ordinal)
                || entry.Message.Contains("Capture cancelled", StringComparison.Ordinal)
                || entry.Message.Contains("Restoration failed", StringComparison.Ordinal)))
        {
            FinishCommand();

            return;
        }

        if (entry.Direction != "RX" || _command is null)
        {
            return;
        }

        CanDiagnosticResult command = _commands[_command];
        if (_monitoring && CanMonitorParser.TryParse(entry.Message, out CanFrame frame))
        {
            _commands[_command] = command with
            {
                Result = "Data received",
                Samples = command.Samples + 1,
                Response = entry.Message,
                Timestamp = entry.Timestamp
            };
            foreach (CanSignalDefinition signal in _module.Signals.Where(signal => signal.CanId == frame.CanId))
            {
                CanDiagnosticResult row = _signals[signal.Name];
                bool complete = signal.StartBit + signal.Length <= frame.Payload.Length * 8;
                _signals[signal.Name] = row with
                {
                    Result = complete ? "Data received (UNVERIFIED)" : "Short payload; not decoded",
                    Samples = row.Samples + 1,
                    Response = entry.Message,
                    RawValue = complete ? signal.ExtractRaw(frame.Payload).ToString(CultureInfo.InvariantCulture) : "",
                    Value = complete ? signal.Decode(frame.Payload).ToString("G", CultureInfo.InvariantCulture) : "",
                    Timestamp = entry.Timestamp
                };
            }
        }
        else if (!_monitoring)
        {
            _commands[_command] = command with
            {
                Result = entry.Message.Split(['\r', '\n'], StringSplitOptions.TrimEntries).Contains("OK")
                    ? "Accepted (not vehicle data)"
                    : "Rejected / adapter response",
                Response = entry.Message,
                Timestamp = entry.Timestamp
            };
        }
        else if (command.Result is not ("Data received" or "Rejected / monitor error"))
        {
            _commands[_command] = command with
            {
                Result = entry.Message.Contains('?') || entry.Message.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                    ? "Rejected / monitor error"
                    : "No frame data",
                Response = entry.Message,
                Timestamp = entry.Timestamp
            };
        }
    }

    private IEnumerable<CanSignalDefinition> MonitoredSignals()
        => _module.Signals.Where(signal => _command == "STM" || signal.CanId == _filter);

    private void FinishCommand()
    {
        if (_command is null)
        {
            return;
        }

        CanDiagnosticResult row = _commands[_command];
        if (row.Result == "Waiting for response")
        {
            _commands[_command] = row with { Result = "No response observed / interrupted" };
        }

        foreach (CanSignalDefinition signal in _module.Signals)
        {
            CanDiagnosticResult reading = _signals[signal.Name];
            if (_observedIds.Contains(signal.CanId) && reading.Samples == 0)
            {
                _signals[signal.Name] = reading with { Result = "No data observed (not proof of unsupported)" };
            }
        }

        _command = null;
        _monitoring = false;
    }

    private void Reset()
    {
        _commands.Clear();
        _signals.Clear();
        _observedIds.Clear();
        _command = null;
        _filter = null;
        _monitoring = false;
        foreach (CanSignalDefinition signal in _module.Signals)
        {
            _signals.Add(signal.Name, new($"CAN {signal.CanId:X3}", signal.Name, "Not attempted",
                0, 0, "", "", "", signal.Unit, null));
        }
    }
}

/// <summary>Contains one command outcome or independently decoded diagnostic signal.</summary>
internal sealed record CanDiagnosticResult(
    string Command, string Reading, string Result, int Attempts, int Samples,
    string Response, string RawValue, string Value, string Unit, DateTimeOffset? Timestamp);
