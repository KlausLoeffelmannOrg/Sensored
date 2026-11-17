using System.Globalization;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Provides grouped V2 columns, independent precision and averaged time-resolution selection.</summary>
internal sealed partial class RawLogViewerDialog
{
    private readonly ToolStripComboBox _resolutionSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripComboBox _v2FileSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Dictionary<string, int> _columnDigits = [];
    private readonly List<RecordingColumn> _v2Columns = [];
    private readonly ContextMenuStrip _precisionMenu = new();
    private V2RecordingTimelineIndex? _v2Index;
    private int _resolutionMilliseconds = 50;
    private string? _loadedPath;
    private bool _updatingSelectors;

    private void SetupTimelineOptions()
    {
        components.Add(_precisionMenu);
        for (int digits = 0; digits <= 9; digits++)
        {
            ToolStripMenuItem item = new($"{digits} decimal places") { Tag = digits };
            item.Click += (_, _) =>
            {
                if (_precisionMenu.Tag is not DataGridViewColumn column)
                {
                    throw new InvalidOperationException("Select a column before changing precision.");
                }

                _columnDigits[column.Name] = (int)item.Tag;
                _grid.InvalidateColumn(column.Index);
            };
            _precisionMenu.Items.Add(item);
        }

        _resolutionSelector.Name = "TimeResolution";
        _resolutionSelector.ToolTipText = "Average valid recorded samples into time buckets; never interpolate missing data.";
        for (int milliseconds = 50; milliseconds <= 1000; milliseconds += 50)
        {
            _resolutionSelector.Items.Add(milliseconds);
        }

        _resolutionSelector.SelectedItem = _resolutionMilliseconds;
        _resolutionSelector.SelectedIndexChanged += OnResolutionChanged;
        _v2FileSelector.Name = "V2File";
        _v2FileSelector.AutoSize = false;
        _v2FileSelector.Width = 280;
        _v2FileSelector.ToolTipText = "Select recorded RawV2 or FuseV2 data; fusion is not recomputed.";
        _v2FileSelector.SelectedIndexChanged += OnV2FileChanged;
        _toolStrip.Items.Insert(3, new ToolStripLabel("Average (ms)"));
        _toolStrip.Items.Insert(4, _resolutionSelector);
        _toolStrip.Items.Insert(5, _v2FileSelector);
        _grid.CellMouseDown += OnPrecisionMenu;
        _grid.CellToolTipTextNeeded += OnV2CellToolTip;
        _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
    }

    private async void OnResolutionChanged(object? sender, EventArgs e)
    {
        if (_updatingSelectors || _resolutionSelector.SelectedItem is not int milliseconds)
        {
            return;
        }

        _resolutionMilliseconds = milliseconds;
        if (_loadedPath is { } path)
        {
            try
            {
                await LoadPathAsync(path);
            }
            catch (Exception exception)
            {
                ShowFailure(exception, "Could not change timeline resolution");
            }
        }
    }

    private async void OnV2FileChanged(object? sender, EventArgs e)
    {
        if (!_updatingSelectors && _v2FileSelector.SelectedItem is RecordingFile file)
        {
            try
            {
                await LoadPathAsync(file.Path);
            }
            catch (Exception exception)
            {
                ShowFailure(exception, "Could not change V2 recording");
            }
        }
    }

    private void OnPrecisionMenu(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.ColumnIndex < (_v2Index is null ? 3 : 2))
        {
            return;
        }

        DataGridViewColumn column = _grid.Columns[e.ColumnIndex];
        _precisionMenu.Tag = column;
        foreach (ToolStripMenuItem item in _precisionMenu.Items)
        {
            item.Checked = item.Tag is int digits && Digits(column.Name) == digits;
        }

        _precisionMenu.Show(Cursor.Position);
    }

    private int Digits(string column) => _columnDigits.GetValueOrDefault(column, 3);

    private string FormatColumn(double value, int column)
        => Format(value, $"F{Digits(_grid.Columns[column].Name)}");

    private async Task LoadV2Async(string path, string[] paths, CancellationToken token)
    {
        _updatingSelectors = true;
        try
        {
            if (Directory.Exists(path) || !_v2FileSelector.Items.Cast<RecordingFile>().Any(file => file.Path == path))
            {
                _v2FileSelector.Items.Clear();
                foreach (string file in paths)
                {
                    _v2FileSelector.Items.Add(new RecordingFile(file));
                }
            }

            string selected = Directory.Exists(path) ? paths.FirstOrDefault(file =>
                file.EndsWith(".RawV2.csv", StringComparison.OrdinalIgnoreCase)) ?? paths[0] : path;
            _loadedPath = selected;
            _v2FileSelector.SelectedItem = _v2FileSelector.Items.Cast<RecordingFile>()
                .First(file => file.Path == selected);
        }
        finally
        {
            _updatingSelectors = false;
        }

        V2RecordingTimelineIndex index = await Task.Run(
            () => V2RecordingTimelineIndex.BuildAsync(_loadedPath!, _resolutionMilliseconds, token), token);
        if (IsDisposed)
        {
            index.Dispose();

            return;
        }

        _v2Index = index;
        BuildV2Columns(index);
        _grid.RowCount = index.Count;
        _editorPanel.Visible = false;
        _statusLabel.Text = $"{index.Count:N0} recorded V2 buckets, {_resolutionMilliseconds} ms; valid-sample means."
            + " Right-click a column for precision. Raw selected row shows original values/provenance; no V2 editing.";
    }

    private void BuildV2Columns(V2RecordingTimelineIndex index)
    {
        _grid.Columns.Clear();
        _v2Columns.Clear();
        _grid.Columns.Add("Time", "Time (local)");
        _grid.Columns.Add("SourceRows", "CSV data rows");
        _grid.Columns[0].Width = 205;
        _grid.Columns[1].Width = 120;
        (string Caption, string[] Fields, string[] Labels)[] groups = index.IsRaw
            ? [
                ("Acceleration (g)", ["accel_x_g", "accel_y_g", "accel_z_g"], ["X", "Y", "Z"]),
                ("Gyroscope (deg/s)", ["gyro_x_dps", "gyro_y_dps", "gyro_z_dps"], ["X", "Y", "Z"]),
                ("Magnetometer (uT)", ["mag_x_ut", "mag_y_ut", "mag_z_ut"], ["X", "Y", "Z"]),
                ("Inclination (deg)", ["incl_pitch_deg", "incl_roll_deg", "incl_yaw_deg"], ["Pitch", "Roll", "Yaw"])
            ]
            : [
                ("Raw acceleration (g)", ["imu_ax", "imu_ay", "imu_az"], ["X", "Y", "Z"]),
                ("Raw gyroscope (deg/s)", ["imu_gx", "imu_gy", "imu_gz"], ["X", "Y", "Z"]),
                ("Raw inclination (deg)", ["imu_pitch_deg", "imu_roll_deg", "imu_yaw_deg"], ["Pitch", "Roll", "Yaw"]),
                ("Fused attitude (deg)", ["pitch_deg", "roll_deg", "heading_deg"], ["Pitch", "Roll", "Heading"]),
                ("Fused acceleration (m/s2)", ["acc_long_mps2", "acc_lat_mps2", "acc_vert_mps2"], ["Forward", "Right", "Down"])
            ];
        HashSet<string> grouped = [];
        foreach (var group in groups)
        {
            if (group.Fields.All(index.Fields.Contains))
            {
                AddColumn(group.Caption, group.Fields, group.Labels);
                grouped.UnionWith(group.Fields);
            }
        }

        IEnumerable<string> visible = index.IsRaw ? SensorV2Schema.Channels : index.Fields.Where(name =>
            name is not ("aux" or "pattern_evidence_json")
            && !name.EndsWith("_t", StringComparison.Ordinal)
            && !name.EndsWith("_sequence", StringComparison.Ordinal)
            && !name.EndsWith("_generation", StringComparison.Ordinal)
            && name is not ("in_t_source" or "in_t_arrival" or "out_t"));
        foreach (string field in visible.Where(field => !grouped.Contains(field) && index.Fields.Contains(field)))
        {
            AddColumn(field, [field], [""]);
        }

        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _grid.RowTemplate.Height = TextRenderer.MeasureText("X\nY\nZ", _grid.Font).Height + 10;

        void AddColumn(string caption, string[] names, string[] labels)
        {
            _v2Columns.Add(new(names.Select(name => Array.IndexOf(index.Fields, name)).ToArray(), labels));
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = names[0],
                HeaderText = caption,
                Width = names.Length > 1 ? 185 : 155,
                ToolTipText = string.Join(", ", names)
                    + ". Mean of finite, valid samples in this bucket; missing samples are not zero."
            });
        }
    }

    private void GetV2CellValue(DataGridViewCellValueEventArgs e)
    {
        V2RecordingTimelineIndex index = _v2Index!;
        if (e.RowIndex < 0 || e.RowIndex >= index.Count)
        {
            return;
        }

        V2TimelineRow row = index.Get(e.RowIndex);
        if (e.ColumnIndex == 0)
        {
            e.Value = new DateTimeOffset(row.StartTicks, TimeSpan.Zero).ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);
        }
        else if (e.ColumnIndex == 1)
        {
            e.Value = $"{row.FirstSourceRow:N0}-{row.LastSourceRow:N0}";
        }
        else
        {
            RecordingColumn column = _v2Columns[e.ColumnIndex - 2];
            e.Value = string.Join(Environment.NewLine, column.Fields.Select((field, component) =>
            {
                string text = row.Values[field];
                text = index.IsMeasurementColumn(field)
                    && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                    ? FormatColumn(value, e.ColumnIndex) : text.Length == 0 ? "-" : text;

                return column.Labels[component].Length > 0 ? $"{column.Labels[component]}: {text}" : text;
            }));
        }
    }

    private void OnV2CellToolTip(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (_v2Index is not { } index || e.RowIndex < 0 || e.ColumnIndex < 2)
        {
            return;
        }

        V2TimelineRow row = index.Get(e.RowIndex);
        RecordingColumn column = _v2Columns[e.ColumnIndex - 2];
        List<string> lines = [];
        foreach (int field in column.Fields)
        {
            string name = index.Fields[field];
            lines.Add(index.IsMeasurementColumn(field)
                ? $"{name}: {row.SampleCounts[field]} valid samples / {row.LastSourceRow - row.FirstSourceRow + 1} rows"
                : $"{name}: latest recorded metadata (not averaged)");
            if (index.IsRaw)
            {
                foreach (string suffix in new[] { "_state", "_t", "_age_s", "_source", "_generation", "_sequence", "_responder" })
                {
                    int metadata = Array.IndexOf(index.Fields, name + suffix);
                    if (metadata >= 0)
                    {
                        lines.Add($"Latest {suffix[1..]}: {row.Values[metadata]}");
                    }
                }
            }
        }

        e.ToolTipText = string.Join(Environment.NewLine, lines);
    }

    private async Task ShowV2SourceAsync()
    {
        if (_v2Index is not { } index || _grid.SelectedRows.Count == 0 || !TryStartOperation())
        {
            return;
        }

        try
        {
            V2TimelineRow selected = index.Get(_grid.SelectedRows[0].Index);
            CancellationToken token = _operationCancellation!.Token;
            string[] values = await Task.Run(
                () => V2RecordingTimelineIndex.ReadSourceRow(index.SourcePath, selected.FirstSourceRow, token), token);
            if (IsDisposed)
            {
                return;
            }

            _rawGrid.Rows.Clear();
            for (int field = 0; field < index.Fields.Length; field++)
            {
                _rawGrid.Rows.Add(index.Fields[field], values[field]);
            }

            _rawGrid.Visible = true;
            _rawButton.Text = "Hide raw row";
            _statusLabel.Text = $"Original CSV data row {selected.FirstSourceRow:N0}; unrounded values, states and provenance."
                + $" Bucket covers rows {selected.FirstSourceRow:N0}-{selected.LastSourceRow:N0}.";
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _statusLabel.Text = "V2 source lookup cancelled.";
            }
        }
        catch (Exception exception)
        {
            ShowFailure(exception, "Could not inspect V2 source row");
        }
        finally
        {
            FinishOperation();
        }
    }

    /// <summary>Maps one displayed column to its ordered scalar components.</summary>
    private sealed record RecordingColumn(int[] Fields, string[] Labels);

    /// <summary>Retains the full path while presenting a short file name in the toolbar.</summary>
    private sealed record RecordingFile(string Path)
    {
        public override string ToString() => System.IO.Path.GetFileName(Path);
    }
}
