using System.Globalization;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.Sensors;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

/// <summary>
///  Browses a time-aligned, disk-backed recording timeline and creates edited copies.
/// </summary>
internal sealed partial class RawLogViewerDialog : Form
{
    private readonly string? _initialPath;
    private readonly string? _preferredDirectory;
    private readonly ReadingFormats _readingFormats;
    private RawRecordingTimelineIndex? _index;
    private string? _lastOpenedPath;
    private string? _sourceFolder;
    private CancellationTokenSource? _operationCancellation;

    /// <summary>
    ///  Initializes a new instance of the <see cref="RawLogViewerDialog"/> class.
    /// </summary>
    internal RawLogViewerDialog(
        ReadingFormats readingFormats,
        DashboardToolbarSize toolbarSize,
        string? initialPath = null,
        string? preferredDirectory = null)
    {
        _readingFormats = readingFormats;
        _initialPath = initialPath;
        _preferredDirectory = preferredDirectory;
        _lastOpenedPath = initialPath;
        InitializeComponent();
        ApplyToolbarAppearance(toolbarSize);
        BuildColumns();
        SetupTimelineOptions();
        Disposed += OnViewerDisposed;
        RefreshCommands();
    }

    /// <summary>Gets the last file or folder opened in this dialog.</summary>
    internal string? LastOpenedPath => _lastOpenedPath;

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (!string.IsNullOrWhiteSpace(_initialPath)
            && (File.Exists(_initialPath) || Directory.Exists(_initialPath)))
        {
            _ = LoadPathAsync(_initialPath);
        }
    }

    /// <inheritdoc/>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _operationCancellation?.Cancel();
        base.OnFormClosed(e);
    }

    private void OnViewerDisposed(object? sender, EventArgs e)
    {
        _operationCancellation?.Cancel();
        _index?.Dispose();
        _v2Index?.Dispose();
    }

    private void ApplyToolbarAppearance(DashboardToolbarSize toolbarSize)
    {
        int symbolSize = (int)toolbarSize;
        SymbolToolStripAppearance.Apply(_toolStrip, new Size(symbolSize, symbolSize));
        _openFileButton.ConfigureItem(
            FluentSymbols.AllSymbols.OpenFile,
            tooltipText: "Open a sensor recording file",
            size: symbolSize);
        _openFolderButton.ConfigureItem(
            FluentSymbols.AllSymbols.FolderOpen,
            tooltipText: "Open a sensor recording folder",
            size: symbolSize);
        _closeButton.ConfigureItem(
            FluentSymbols.AllSymbols.Cancel,
            tooltipText: "Close the recording viewer",
            size: symbolSize);
    }

    private void BuildColumns()
    {
        (string Name, string Header, int Width)[] columns =
        [
            ("Time", "Arrival time (local)", 205),
            ("Part", "File part / section", 300),
            ("SourceRows", "Source rows", 155),
            ("Speed", "Speed (km/h)", 120),
            ("SpeedSource", "Speed source", 100),
            ("Course", "Raw GPS course (°)", 145),
            ("Pitch", "Pitch (°)", 100),
            ("Roll", "Roll (°)", 100),
            ("Yaw", "Fused heading / yaw (°)", 175),
            ("Forward", "Forward (+) (g)", 125),
            ("Up", "Up (g)", 90),
            ("Down", "Down (g)", 90),
            ("Side", "Side (+right) (g)", 145)
        ];

        foreach ((string name, string header, int width) in columns)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                ToolTipText = name == "Part"
                    ? "A ▶ and a horizontal divider mark the first timeline row of each recording part."
                    : string.Empty,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        _rawGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Raw field",
            Width = 155,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _rawGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Value",
            Width = 200,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void OnOpenFileClick(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            Filter = "Sensor recordings (*.jsonl;*.compact.json;*.csv)|*.jsonl;*.compact.json;*.csv",
            InitialDirectory = PreferredDirectory(),
            Title = "Open sensor recording part"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _lastOpenedPath = dialog.FileName;
            _ = LoadPathAsync(dialog.FileName);
        }
    }

    private void OnOpenFolderClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Open a service-owned sensor recording folder",
            InitialDirectory = PreferredDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _lastOpenedPath = dialog.SelectedPath;
            _ = LoadPathAsync(dialog.SelectedPath);
        }
    }

    private void OnCloseClick(object? sender, EventArgs e) => Close();

    private void OnCancelClick(object? sender, EventArgs e) => _operationCancellation?.Cancel();

    private async void OnRawClick(object? sender, EventArgs e)
    {
        if (_rawGrid.Visible)
        {
            _rawGrid.Visible = false;
            _rawButton.Text = "Raw selected row";
            return;
        }

        if (_v2Index is not null)
        {
            await ShowV2SourceAsync();

            return;
        }

        if (_index is null || _grid.SelectedRows.Count == 0)
        {
            _statusLabel.Text = "Select a timeline row to inspect its first original source row.";
            return;
        }

        TimelineSnapshot selected = _index.Get(_grid.SelectedRows[0].Index);
        string path = _index.PartPath(selected.PartIndex);
        long sourceRow = selected.FirstSourceRow;
        if (!TryStartOperation())
        {
            return;
        }

        try
        {
            _statusLabel.Text = $"Reading source row {sourceRow:N0} from {Path.GetFileName(path)}...";
            CancellationToken token = _operationCancellation!.Token;
            SensorEnvelopeRow? original = await Task.Run(async () =>
            {
                await foreach (SensorEnvelopeRow row in SensorRecordingReader
                    .ReadPartAsync(path, token).ConfigureAwait(false))
                {
                    if (row.Row == sourceRow)
                    {
                        return row;
                    }
                }

                return null;
            }, token);
            if (IsDisposed)
            {
                return;
            }

            if (original is null)
            {
                throw new InvalidDataException("The selected source row is no longer present.");
            }

            _rawGrid.Rows.Clear();
            for (int field = 0; field < SensorRecordingSchema.FieldCount; field++)
            {
                SensorRecordingField definition = SensorRecordingSchema.Fields[field];
                object? value = SensorRecordingSchema.GetValue(original, field);
                _rawGrid.Rows.Add(definition.Name, FormatRawValue(value, definition));
            }

            _rawGrid.Visible = true;
            _rawButton.Text = "Hide raw row";
            _statusLabel.Text = $"Original source row {sourceRow:N0} in {Path.GetFileName(path)} (all schema fields).";
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _statusLabel.Text = "Raw row lookup cancelled.";
            }
        }
        catch (Exception exception)
        {
            ShowFailure(exception, "Could not inspect raw source row");
        }
        finally
        {
            FinishOperation();
        }
    }

    private string FormatRawValue(object? value, SensorRecordingField field)
        => value switch
        {
            null => string.Empty,
            DateTimeOffset timestamp => timestamp.ToString("O", CultureInfo.CurrentCulture),
            double number => Format(number, field.Name switch
            {
                "geo_lat" or "geo_lon" => _readingFormats.Gps,
                "geo_speed_mps" => _readingFormats.Speed,
                _ when field.Name.StartsWith("imu_a", StringComparison.Ordinal)
                    => _readingFormats.Acceleration,
                _ => _readingFormats.Rest
            }),
            _ => SensorRecordingSchema.FormatValue(value, field.Type)
        };

    private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (_v2Index is not null)
        {
            GetV2CellValue(e);

            return;
        }

        if (_index is null || e.RowIndex < 0 || e.RowIndex >= _index.Count)
        {
            return;
        }

        TimelineSnapshot row = _index.Get(e.RowIndex);
        e.Value = e.ColumnIndex switch
        {
            0 => row.Start.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture),
            1 => IsPartStart(e.RowIndex, row)
                ? $"▶  {_index.PartName(row.PartIndex)}"
                : _index.PartName(row.PartIndex),
            2 => row.FirstSourceRow == row.LastSourceRow
                ? row.FirstSourceRow.ToString("N0", CultureInfo.CurrentCulture)
                : $"{row.FirstSourceRow:N0}–{row.LastSourceRow:N0}",
            3 => FormatColumn(row.Speed * 3.6d, e.ColumnIndex),
            4 => row.SpeedSource switch { 2 => "Fused", 1 => "Raw GPS", _ => "Invalid" },
            5 => FormatColumn(row.Course, e.ColumnIndex),
            6 => FormatColumn(row.Pitch, e.ColumnIndex),
            7 => FormatColumn(row.Roll, e.ColumnIndex),
            8 => FormatColumn(row.Yaw, e.ColumnIndex),
            9 => FormatColumn(row.Forward, e.ColumnIndex),
            10 => FormatColumn(row.Up, e.ColumnIndex),
            11 => FormatColumn(row.Down, e.ColumnIndex),
            12 => FormatColumn(row.Side, e.ColumnIndex),
            _ => null
        };
    }

    private static string Format(double value, string numberFormat)
        => double.IsFinite(value)
            ? value.ToString(numberFormat, CultureInfo.CurrentCulture) : "—";

    private bool IsPartStart(int index, TimelineSnapshot row)
        => index == 0 || _index!.Get(index - 1).PartIndex != row.PartIndex;

    private void OnRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
    {
        if (_index is null || e.RowIndex < 0 || e.RowIndex >= _index.Count
            || !IsPartStart(e.RowIndex, _index.Get(e.RowIndex)))
        {
            return;
        }

        using Pen divider = new(SystemColors.Highlight, 2f);
        int y = e.RowBounds.Top + 1;
        e.Graphics.DrawLine(divider, e.RowBounds.Left, y, e.RowBounds.Right - 1, y);
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_index is null || e.RowIndex < 0 || e.RowIndex >= _index.Count)
        {
            return;
        }

        TimelineSnapshot row = _index.Get(e.RowIndex);
        DateTimeOffset start = row.Start;
        DateTimeOffset end = row.End;

        foreach (object item in _rangeList.CheckedItems)
        {
            if (item is EditSelection cut && cut.Range.Start <= end && cut.Range.End >= start)
            {
                e.CellStyle.BackColor = SystemColors.Info;
                e.CellStyle.ForeColor = SystemColors.InfoText;
                break;
            }
        }
    }

    private async Task LoadPathAsync(string path)
    {
        if (!TryStartOperation())
        {
            return;
        }

        CancellationToken token = _operationCancellation!.Token;
        _grid.RowCount = 0;
        _rawGrid.Visible = false;
        _rawGrid.Rows.Clear();
        _rawButton.Text = "Raw selected row";
        _index?.Dispose();
        _index = null;
        _v2Index?.Dispose();
        _v2Index = null;
        _sourceFolder = null;
        _rangeList.Items.Clear();
        _pathLabel.Text = path;
        _loadedPath = path;
        _statusLabel.Text = $"Building {_resolutionMilliseconds} ms averaged timeline index...";
        RefreshCommands();

        try
        {
            string[] v2Files = V2RecordingTimelineIndex.FindFiles(path);
            if (v2Files.Length > 0)
            {
                await LoadV2Async(path, v2Files, token);

                return;
            }

            _updatingSelectors = true;
            _v2FileSelector.Items.Clear();
            _updatingSelectors = false;
            _grid.Columns.Clear();
            _rawGrid.Columns.Clear();
            BuildColumns();
            _grid.RowTemplate.Height = _grid.Font.Height + 9;
            _editorPanel.Visible = true;
            RawRecordingTimelineIndex index = await Task.Run(
                () => RawRecordingTimelineIndex.BuildAsync(path, token, _resolutionMilliseconds), token);
            if (IsDisposed)
            {
                index.Dispose();
                return;
            }

            _index = index;
            _grid.RowCount = index.Count;
            _sourceFolder = Directory.Exists(path) ? Path.GetFullPath(path) : null;
            _statusLabel.Text = $"{index.Count:N0} time-aligned rows ({_resolutionMilliseconds} ms); — means invalid or stale."
                + (_sourceFolder is null || !CanEdit(_sourceFolder)
                    ? " Editing requires a recording folder with a manifest and CSV parts."
                    : string.Empty);
            RefreshCommands();

            if (_sourceFolder is not null && CanEdit(_sourceFolder))
            {
                try
                {
                    await DetectRangesAsync(token);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    ShowFailure(exception, "Could not detect stationary ranges");
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _statusLabel.Text = "Operation cancelled.";
            }
        }
        catch (Exception exception)
        {
            ShowFailure(exception, "Could not open sensor recording");
        }
        finally
        {
            FinishOperation();
        }
    }

    private async void OnDetectClick(object? sender, EventArgs e)
    {
        if (_sourceFolder is null || !TryStartOperation())
        {
            return;
        }

        try
        {
            await DetectRangesAsync(_operationCancellation!.Token);
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _statusLabel.Text = "Stationary detection cancelled.";
            }
        }
        catch (Exception exception)
        {
            ShowFailure(exception, "Could not detect stationary ranges");
        }
        finally
        {
            FinishOperation();
        }
    }

    private async Task DetectRangesAsync(CancellationToken token)
    {
        if (_sourceFolder is null)
        {
            return;
        }

        _statusLabel.Text = "Scanning GPS fixes for stationary intervals...";
        IReadOnlyList<SensorRecordingEditRange> ranges = await Task.Run(
            () => SensorRecordingEditor.FindStationaryRangesAsync(_sourceFolder, token), token);
        if (IsDisposed)
        {
            return;
        }

        for (int i = _rangeList.Items.Count - 1; i >= 0; i--)
        {
            if (_rangeList.Items[i] is EditSelection { Automatic: true })
            {
                _rangeList.Items.RemoveAt(i);
            }
        }

        foreach (SensorRecordingEditRange range in ranges)
        {
            _rangeList.Items.Add(new EditSelection(range, Automatic: true), isChecked: true);
        }

        _statusLabel.Text = $"{ranges.Count:N0} stationary ranges suggested; review checked cuts before Save As.";
        RefreshCommands();
        _grid.Invalidate();
    }

    private void OnSelectRangeClick(object? sender, EventArgs e)
    {
        if (_index is null || _grid.SelectedRows.Count == 0)
        {
            _statusLabel.Text = "Select one or more timeline rows first.";
            return;
        }

        int first = int.MaxValue;
        int last = -1;

        foreach (DataGridViewRow selected in _grid.SelectedRows)
        {
            first = Math.Min(first, selected.Index);
            last = Math.Max(last, selected.Index);
        }

        _startPicker.Value = _index.Get(first).Start.LocalDateTime;
        _endPicker.Value = _index.Get(last).End.LocalDateTime.AddMilliseconds(1);
    }

    private void OnAddRangeClick(object? sender, EventArgs e)
    {
        DateTimeOffset start = new(_startPicker.Value, TimeZoneInfo.Local.GetUtcOffset(_startPicker.Value));
        DateTimeOffset end = new(_endPicker.Value, TimeZoneInfo.Local.GetUtcOffset(_endPicker.Value));

        if (end <= start)
        {
            _statusLabel.Text = "End must be later than start (local time).";
            return;
        }

        _rangeList.Items.Add(
            new EditSelection(new SensorRecordingEditRange(start, end), Automatic: false),
            isChecked: true);
        RefreshCommands();
        _grid.Invalidate();
    }

    private void OnRemoveRangeClick(object? sender, EventArgs e)
    {
        if (_rangeList.SelectedIndex >= 0)
        {
            _rangeList.Items.RemoveAt(_rangeList.SelectedIndex);
            RefreshCommands();
            _grid.Invalidate();
        }
    }

    private void OnRangeItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // CheckedItems is updated only after ItemCheck returns.
        BeginInvoke(() =>
        {
            if (!IsDisposed)
            {
                RefreshCommands();
                _grid.Invalidate();
            }
        });
    }

    private async void OnSaveCopyClick(object? sender, EventArgs e)
    {
        if (_sourceFolder is null)
        {
            return;
        }

        SensorRecordingEditRange[] cuts = GetMergedCuts();
        if (cuts.Length == 0)
        {
            _statusLabel.Text = "Check at least one range to remove.";
            return;
        }

        using FolderBrowserDialog dialog = new()
        {
            Description = "Choose the PARENT folder for a new recording copy (the source is never changed)",
            InitialDirectory = Path.GetDirectoryName(_sourceFolder) ?? PreferredDirectory()
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string parent = Path.GetFullPath(dialog.SelectedPath);
        string source = Path.GetFullPath(_sourceFolder).TrimEnd(Path.DirectorySeparatorChar);
        if (IsWithin(parent, source))
        {
            _statusLabel.Text = "Choose an existing folder outside the source recording.";
            return;
        }

        string stem = Path.GetFileName(source) + "-edited-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string destination = Path.Combine(parent, stem);
        int suffix = 2;
        while (Directory.Exists(destination) || File.Exists(destination))
        {
            destination = Path.Combine(parent, $"{stem}-{suffix++}");
        }

        if (MessageBox.Show(this,
            $"Remove {cuts.Length:N0} time range(s) and save a NEW copy to:\n{destination}\n\nThe original will not be modified.",
            "Confirm Save As copy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes
            || !TryStartOperation())
        {
            return;
        }

        try
        {
            _statusLabel.Text = "Saving and verifying new recording copy...";
            string folder = _sourceFolder;
            CancellationToken token = _operationCancellation!.Token;
            SensorRecordingEditResult result = await Task.Run(
                () => SensorRecordingEditor.SaveCopyAsync(folder, destination, cuts, token), token);

            if (!IsDisposed)
            {
                _statusLabel.Text =
                    $"Saved {result.OutputRows:N0} of {result.InputRows:N0} rows; removed {result.RemovedDuration:g}.";
                MessageBox.Show(this, $"New recording:\n{result.Folder}",
                    "Recording copy saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _statusLabel.Text = "Save cancelled; the original remains unchanged.";
            }
        }
        catch (Exception exception)
        {
            ShowFailure(exception, "Could not save recording copy");
        }
        finally
        {
            FinishOperation();
        }
    }

    private SensorRecordingEditRange[] GetMergedCuts()
    {
        SensorRecordingEditRange[] selected =
        [
            .. _rangeList.CheckedItems.Cast<EditSelection>()
                .Select(static item => item.Range)
                .OrderBy(static range => range.Start)
        ];
        List<SensorRecordingEditRange> merged = [];

        foreach (SensorRecordingEditRange range in selected)
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End)
            {
                SensorRecordingEditRange previous = merged[^1];
                merged[^1] = previous with { End = range.End > previous.End ? range.End : previous.End };
            }
            else
            {
                merged.Add(range);
            }
        }

        return [.. merged];
    }

    private static bool CanEdit(string folder)
        => File.Exists(Path.Combine(folder, "recording.manifest.json"))
            && Directory.EnumerateFiles(folder, "part-*.csv")
                .Any(static file => !Path.GetFileName(file).Contains("--writing", StringComparison.Ordinal));

    private static bool IsWithin(string path, string ancestor)
        => path.Equals(ancestor, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private bool TryStartOperation()
    {
        if (_operationCancellation is not null || IsDisposed)
        {
            return false;
        }

        _operationCancellation = new CancellationTokenSource();
        RefreshCommands();
        return true;
    }

    private void FinishOperation()
    {
        _operationCancellation?.Dispose();
        _operationCancellation = null;

        if (!IsDisposed)
        {
            RefreshCommands();
        }
    }

    private void RefreshCommands()
    {
        bool busy = _operationCancellation is not null;
        bool editable = _sourceFolder is not null && CanEdit(_sourceFolder);
        _openFileButton.Enabled = !busy;
        _openFolderButton.Enabled = !busy;
        _rawButton.Enabled = !busy && (_index is { Count: > 0 } || _v2Index is { Count: > 0 });
        _resolutionSelector.Enabled = !busy;
        _v2FileSelector.Enabled = !busy;
        _detectButton.Enabled = !busy && editable;
        _saveCopyButton.Enabled = !busy && editable && _rangeList.CheckedItems.Count > 0;
        _cancelButton.Enabled = busy;
        _editButtons.Enabled = !busy && _index is not null && editable;
        _rangeList.Enabled = !busy && editable;
    }

    private void ShowFailure(Exception exception, string title)
    {
        if (IsDisposed)
        {
            return;
        }

        _statusLabel.Text = $"{title}: {exception.Message}";
        MessageBox.Show(this, exception.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private string PreferredDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_lastOpenedPath))
        {
            string? openedDirectory = Directory.Exists(_lastOpenedPath)
                ? _lastOpenedPath : Path.GetDirectoryName(_lastOpenedPath);
            if (!string.IsNullOrWhiteSpace(openedDirectory) && Directory.Exists(openedDirectory))
            {
                return openedDirectory;
            }
        }

        if (!string.IsNullOrWhiteSpace(_preferredDirectory) && Directory.Exists(_preferredDirectory))
        {
            return _preferredDirectory;
        }

        Directory.CreateDirectory(MainForm.RecordingsRootFolder);
        return MainForm.RecordingsRootFolder;
    }

    /// <summary>
    ///  Identifies a proposed or user-defined removal interval in the preview.
    /// </summary>
    private sealed record EditSelection(SensorRecordingEditRange Range, bool Automatic)
    {
        public override string ToString()
            => $"{(Automatic ? "Stationary suggestion" : "Manual cut")}: "
                + $"{Range.Start.LocalDateTime:yyyy-MM-dd HH:mm:ss.fff} – "
                + $"{Range.End.LocalDateTime:yyyy-MM-dd HH:mm:ss.fff} (local, {Range.End - Range.Start:g})";
    }
}
