using System.Globalization;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.Sensors;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

/// <summary>
///  Displays every field of current service-owned recording files by using the recording schema.
/// </summary>
internal partial class RawLogViewerDialog : Form
{
    private readonly List<SensorEnvelopeRow> _rows = [];
    private readonly string? _initialPath;
    private readonly ReadingFormats _readingFormats;
    private CancellationTokenSource? _loadCancellation;

    /// <summary>
    ///  Initializes a new instance of the <see cref="RawLogViewerDialog"/> class.
    /// </summary>
    /// <param name="readingFormats">The number formats used to display recorded values.</param>
    /// <param name="toolbarSize">The dashboard command icon size.</param>
    /// <param name="initialPath">An optional recording folder or part file to load.</param>
    internal RawLogViewerDialog(
        ReadingFormats readingFormats,
        DashboardToolbarSize toolbarSize,
        string? initialPath = null)
    {
        _readingFormats = readingFormats;
        _initialPath = initialPath;
        InitializeComponent();
        ApplyToolbarAppearance(toolbarSize);
        BuildColumns();
    }

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
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        base.OnFormClosed(e);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "#",
            Name = "_indexColumn",
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Width = 70
        });

        foreach (SensorRecordingField field in SensorRecordingSchema.Fields)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = field.Name,
                Name = field.Name,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = field.Type == SensorRecordingFieldType.Timestamp ? 190 : 110
            });
        }
    }

    private void OnOpenFileClick(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            Filter =
                "Sensor recordings (*.jsonl;*.compact.json;*.csv)|*.jsonl;*.compact.json;*.csv|"
                + "All files (*.*)|*.*",
            InitialDirectory = DefaultRecordingDirectory(),
            Title = "Open sensor recording part"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadPathAsync(dialog.FileName);
        }
    }

    private void OnOpenFolderClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Open a service-owned sensor recording folder",
            InitialDirectory = DefaultRecordingDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = LoadPathAsync(dialog.SelectedPath);
        }
    }

    private void OnCloseClick(object? sender, EventArgs e)
        => Close();

    private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if ((uint)e.RowIndex >= (uint)_rows.Count)
        {
            return;
        }

        if (e.ColumnIndex == 0)
        {
            e.Value = (e.RowIndex + 1).ToString("N0", CultureInfo.CurrentCulture);

            return;
        }

        int fieldIndex = e.ColumnIndex - 1;
        SensorRecordingField field = SensorRecordingSchema.Fields[fieldIndex];
        object? value = SensorRecordingSchema.GetValue(_rows[e.RowIndex], fieldIndex);
        e.Value = FormatValue(value, field);
    }

    private async Task LoadPathAsync(string path)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadCancellation.Token;

        _openFileButton.Enabled = false;
        _openFolderButton.Enabled = false;
        _rows.Clear();
        _grid.RowCount = 0;
        _statusLabel.Text = "Loading recording...";
        _pathLabel.Text = path;

        try
        {
            SensorRecordingFormats format = Directory.Exists(path)
                ? DetectFolderFormat(path)
                : SensorRecordingReader.DetectFormat(path);
            IAsyncEnumerable<SensorEnvelopeRow> rows = Directory.Exists(path)
                ? SensorRecordingReader.ReadFolderAsync(path, format, cancellationToken)
                : SensorRecordingReader.ReadPartAsync(path, cancellationToken);

            await foreach (SensorEnvelopeRow row in rows.ConfigureAwait(true))
            {
                _rows.Add(row);

                if ((_rows.Count & 1023) == 0)
                {
                    _grid.RowCount = _rows.Count;
                    _statusLabel.Text = $"{_rows.Count:N0} rows loaded...";
                    await Task.Yield();
                }
            }

            _grid.RowCount = _rows.Count;
            _statusLabel.Text = $"{_rows.Count:N0} rows loaded.";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Loading cancelled.";
        }
        catch (Exception exception)
        {
            _statusLabel.Text = $"Load failed: {exception.Message}";
            MessageBox.Show(
                this,
                exception.Message,
                "Could not open sensor recording",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _openFileButton.Enabled = true;
            _openFolderButton.Enabled = true;
        }
    }

    private string FormatValue(object? value, SensorRecordingField field)
        => value switch
        {
            null => string.Empty,
            DateTimeOffset timestamp => timestamp.ToString("O", CultureInfo.CurrentCulture),
            double number => number.ToString(SelectNumberFormat(field.Name), CultureInfo.CurrentCulture),
            _ => SensorRecordingSchema.FormatValue(value, field.Type)
        };

    private string SelectNumberFormat(string fieldName)
    {
        if (fieldName.StartsWith("imu_a", StringComparison.Ordinal))
        {
            return _readingFormats.Acceleration;
        }

        if (fieldName is "geo_lat" or "geo_lon")
        {
            return _readingFormats.Gps;
        }

        if (fieldName == "geo_speed_mps")
        {
            return _readingFormats.Speed;
        }

        return _readingFormats.Rest;
    }

    private static string DefaultRecordingDirectory()
    {
        Directory.CreateDirectory(MainForm.RecordingsRootFolder);

        return MainForm.RecordingsRootFolder;
    }

    private static SensorRecordingFormats DetectFolderFormat(string folderPath)
    {
        if (Directory.EnumerateFiles(folderPath, "part-*.jsonl").Any())
        {
            return SensorRecordingFormats.VerboseJsonLines;
        }

        if (Directory.EnumerateFiles(folderPath, "part-*.compact.json").Any())
        {
            return SensorRecordingFormats.CompactJson;
        }

        if (Directory.EnumerateFiles(folderPath, "part-*.csv").Any())
        {
            return SensorRecordingFormats.SemicolonCsv;
        }

        throw new FileNotFoundException(
            "The folder does not contain a finalized sensor recording part.",
            folderPath);
    }
}
