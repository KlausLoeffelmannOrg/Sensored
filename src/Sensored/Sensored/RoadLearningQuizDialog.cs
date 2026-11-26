namespace Sensored;

/// <summary>Passenger-operated, modeless quiz. Viewing evidence discards the frozen question.</summary>
internal sealed class RoadLearningQuizDialog : Form
{
    private readonly RoadLearningSession _session;
    private readonly RoadLearningJournal _journal;
    private readonly Func<bool> _canContinue;
    private readonly Func<string?> _recordingPath;
    private readonly Action<string, Exception> _reportError;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 500 };
    private readonly Label _confidence = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Label _condition = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Label _notice = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _answers = new() { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly CheckBox _undo = new() { Text = "Disregard Last", AutoSize = true, Enabled = false };
    private readonly Button _pause = new() { AutoSize = true };
    private readonly Button _less = new() { Text = "<<", AutoSize = true };
    private readonly Button _more = new() { Text = ">>", AutoSize = true };
    private readonly Button _end = new() { Text = "End Quizz", AutoSize = true };
    private readonly StatusStrip _sources = new() { SizingGrip = false, ShowItemToolTips = true };
    private readonly Font _headingFont;
    private readonly Font _answerFont;
    private readonly Font _controlFont;
    private RoadLearningQuestion? _question;
    private Task? _pendingWrite;
    private bool _busy;
    private bool _detailsOpen;
    private bool _ending;
    private int _pauseMinutes = 5;

    internal RoadLearningQuizDialog(RoadLearningSession session, RoadLearningJournal journal,
        Func<bool> canContinue, Func<string?> recordingPath, Action<string, Exception> reportError, Font ownerFont)
    {
        _session = session;
        _journal = journal;
        _canContinue = canContinue;
        _recordingPath = recordingPath;
        _reportError = reportError;
        Font = ownerFont;
        _headingFont = new(ownerFont.FontFamily, Math.Max(26, ownerFont.SizeInPoints * 1.7f), FontStyle.Bold);
        _answerFont = new(ownerFont.FontFamily, Math.Max(20, ownerFont.SizeInPoints * 1.2f));
        _controlFont = new(ownerFont.FontFamily, Math.Max(16, ownerFont.SizeInPoints));
        Text = "Road learning Quizz - passenger use only";
        Name = nameof(RoadLearningQuizDialog);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1100, 850);
        MinimumSize = new Size(680, 500);
        ShowInTaskbar = false;
        _condition.Font = _headingFont;
        _answers.Font = _answerFont;
        _condition.Text = "Learning session started";
        _notice.Text = "Passenger use only; do not operate while driving. Waiting for 30 seconds of continuous live data.\r\n"
            + "Confidence is an experimental rule score, NOT a calibrated probability. Levels are subjective.\r\n"
            + "No map/sign/gear data: stop sign vs light, reverse and parking are motion-only guesses - please correct them.\r\n"
            + "Body tilt only approximates road grade; hill levels are provisional.";
        _confidence.Text = "My guess - Confidence -- %:";
        TableLayoutPanel content = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(16) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.Controls.Add(_confidence);
        content.Controls.Add(_condition);
        content.Controls.Add(_notice);
        content.Controls.Add(_answers);
        for (int level = 1; level <= 5; level++)
        {
            AddAnswer(level, $"Level {level}");
        }

        AddAnswer(0, "0 - Wrong assessment");
        Panel viewport = new() { Dock = DockStyle.Fill, AutoScroll = true };
        viewport.Controls.Add(content);
        TableLayoutPanel bottom = new() { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 3, Padding = new Padding(10) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        _undo.Font = _answerFont;
        _undo.Anchor = AnchorStyles.Left;
        _undo.CheckedChanged += OnUndo;
        FlowLayoutPanel pauseControls = new() { AutoSize = true, Anchor = AnchorStyles.None, WrapContents = false };
        _less.Font = _controlFont;
        _pause.Font = _controlFont;
        _more.Font = _controlFont;
        pauseControls.Controls.AddRange([_less, _pause, _more]);
        _less.Click += (_, _) => { _pauseMinutes = Math.Max(1, _pauseMinutes - 1); UpdatePause(); };
        _more.Click += (_, _) => { _pauseMinutes = Math.Min(20, _pauseMinutes + 1); UpdatePause(); };
        _pause.Click += (_, _) =>
        {
            _session.Pause(DateTimeOffset.UtcNow, _pauseMinutes);
            DismissQuestion($"Paused until {_session.PausedUntil:t}.");
        };
        _end.Font = _answerFont;
        _end.Anchor = AnchorStyles.Right;
        _end.Click += (_, _) => Close();
        bottom.Controls.Add(_undo, 0, 0);
        bottom.Controls.Add(pauseControls, 1, 0);
        bottom.Controls.Add(_end, 2, 0);
        Controls.Add(viewport);
        Controls.Add(bottom);
        Controls.Add(_sources);
        foreach (string abbreviation in new[] { "GPS v", "GPS a", "VEH v/a/RPM", "INT acc/gyro", "BT acc/gyro" })
        {
            ToolStripStatusLabel item = new($"{abbreviation}: --") { IsLink = true, Spring = true };
            item.Click += (_, _) => ShowEvidence();
            _sources.Items.Add(item);
        }

        UpdatePause();
        _answers.Enabled = false;
        _timer.Tick += (_, _) => Poll(DateTimeOffset.UtcNow);
        _timer.Start();
    }

    private void AddAnswer(int level, string text)
    {
        RadioButton answer = new()
        {
            Name = $"Level{level}",
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(420, 58),
            Margin = new Padding(8, level == 0 ? 24 : 5, 8, 5),
            Tag = level,
            AccessibleName = text
        };
        answer.Click += OnAnswer;
        _answers.Controls.Add(answer);
    }

    protected override void OnShown(EventArgs e)
    {
        Rectangle area = Screen.FromControl(Owner ?? this).WorkingArea;
        MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
        if (StartPosition != FormStartPosition.Manual)
        {
            Size target = new((int)(area.Width * 0.7), (int)(area.Height * 0.7));
            MinimumSize = new Size(Math.Min(MinimumSize.Width, target.Width), Math.Min(MinimumSize.Height, target.Height));
            Bounds = new Rectangle(area.Left + (area.Width - target.Width) / 2,
                area.Top + (area.Height - target.Height) / 2, target.Width, target.Height);
        }
        else
        {
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        }

        Poll(DateTimeOffset.UtcNow);
        base.OnShown(e);
    }

    internal void Poll(DateTimeOffset now)
    {
        if (!_canContinue() || !_session.Active)
        {
            Close();
            return;
        }

        if (_busy || _detailsOpen)
        {
            return;
        }

        _undo.Enabled = _journal.CanUndo;
        if (_question is { } current && !_session.IsCurrent(current, now))
        {
            DismissQuestion("Question expired; waiting for a different condition.");
        }

        if (now < _session.PausedUntil)
        {
            _condition.Text = $"Paused - resumes at {_session.PausedUntil:t}";
            return;
        }

        if (_question is not null)
        {
            return;
        }

        if (_session.TryQuestion(now, _recordingPath()) is not { } question)
        {
            UpdateProgress(now);
            return;
        }

        _question = question;
        _confidence.Text = $"My guess - Confidence {question.Confidence} %: (experimental rule score)";
        _condition.Text = RoadConditions.Headline(question.Condition, question.Level);
        _notice.Text = $"Last 30 seconds, ending {question.End:T}. Tap your level, or 0 if the condition is wrong.\r\n"
            + "1 = soft / gentle / Grandpa; 5 = very rough / strong / steep / Schumacher. Passenger use only.\r\n"
            + "No answer is recorded until you tap. Tap a status field for evidence (dismisses this question).";
        foreach (RadioButton answer in _answers.Controls.OfType<RadioButton>())
        {
            answer.Checked = false;
        }

        _answers.Enabled = true;
        for (int index = 0; index < _sources.Items.Count; index++)
        {
            RoadSourceEvidence source = question.Sources[index < 2 ? 0 : index - 1];
            string prefix = index switch { 0 => "GPS v", 1 => "GPS a", _ => source.Abbreviation };
            _sources.Items[index].Text = index switch
            {
                0 => $"{prefix}: {Format(source.Speed)} m/s",
                1 => $"{prefix}: {Format(source.Acceleration)} m/s2",
                _ => $"{prefix}: {source.Contribution:0}%"
            };
            _sources.Items[index].ToolTipText = $"{source.Sensors}: {source.Contribution:0}% of rule evidence; {source.Explanation}";
        }
    }

    private void UpdateProgress(DateTimeOffset now)
    {
        RoadLearningProgress progress = _session.GetProgress(now);
        _condition.Text = progress.State switch
        {
            RoadLearningWaitState.WaitingForData => "Waiting for live sensor data - 30 seconds remaining",
            RoadLearningWaitState.Collecting => $"Collecting live sensor data - {progress.SecondsRemaining} seconds remaining",
            RoadLearningWaitState.WaitingForSpeed => "Quiz running - waiting for GPS or OBD2 speed",
            RoadLearningWaitState.ConflictingSpeed => "Quiz running - GPS and OBD2 speed evidence disagree",
            RoadLearningWaitState.ConfirmingCondition => "Quiz running - confirming a detected condition",
            RoadLearningWaitState.WaitingForDifferentCondition => "Quiz running - waiting for a different condition",
            _ => "Quiz running - waiting for a recognizable motion condition"
        };
        _notice.Text = progress.State switch
        {
            RoadLearningWaitState.WaitingForData => "No fresh usable samples. Check live acquisition; the countdown restarts after a data gap.",
            RoadLearningWaitState.WaitingForSpeed => "The 30-second context is ready, but motion rules need fresh measured speed. Check the GPS fix or OBD2 connection.",
            RoadLearningWaitState.ConflictingSpeed => "The 30-second context is ready. Questions are withheld until the speed sources agree.",
            _ => $"{progress.Samples} usable samples in the rolling 30-second context. Questions appear after a condition remains stable for 2 seconds."
        };
        _notice.Text += "\r\nPassenger use only; do not operate while driving. Confidence is an experimental rule score, not calibrated accuracy.";
    }

    private async void OnAnswer(object? sender, EventArgs e)
    {
        if (_busy || _question is not { } question || sender is not RadioButton { Tag: int level })
        {
            return;
        }

        if (!_session.IsCurrent(question, DateTimeOffset.UtcNow))
        {
            DismissQuestion("Evidence changed or expired; waiting for a different condition.");
            return;
        }

        await SaveAnswerAsync(question, level).ConfigureAwait(true);
    }

    internal object GetAutomationState() => new
    {
        Active = _session.Active,
        ContinuityBreaks = _session.ContinuityBreaks,
        LastContinuityBreak = _session.LastContinuityBreak,
        Condition = _condition.Text,
        Notice = _notice.Text,
        Busy = _busy,
        CanAnswer = _answers.Enabled && _question is { } current && _session.IsCurrent(current, DateTimeOffset.UtcNow),
        Question = _question is { } question ? new
        {
            question.Id,
            Condition = question.Condition.ToString(),
            question.Level,
            question.Confidence,
            question.End,
            ExpiresAt = question.End + RoadLearningSession.QuestionLifetime,
            Sources = question.Sources.Select(source => new
            {
                source.Abbreviation, source.Sensors, source.Samples, source.Speed, source.Acceleration,
                source.Contribution, source.Explanation
            }),
            SampleLagSeconds = question.Samples.GroupBy(sample => sample.Source).ToDictionary(
                group => group.Key.ToString(),
                group => Math.Round(group.Average(sample => (sample.ReceivedAt - sample.MeasuredAt).TotalSeconds), 2))
        } : null
    };

    internal async Task AnswerAsync(Guid questionId, int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 5);
        if (_busy || _detailsOpen || _question is not { } question || question.Id != questionId
            || !_session.IsCurrent(question, DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("The requested quiz question is unavailable, expired or changed.");
        }

        if (!await SaveAnswerAsync(question, level).ConfigureAwait(true))
        {
            throw new IOException("The quiz answer was not saved; see the quiz error notice.");
        }
    }

    private Task<bool> SaveAnswerAsync(RoadLearningQuestion question, int level)
        => PersistAsync(() => _journal.AnswerAsync(question, level, DateTimeOffset.UtcNow),
            () => DismissQuestion("Answer saved. Waiting for a different condition."));

    private async void OnUndo(object? sender, EventArgs e)
    {
        if (!_undo.Checked || _busy)
        {
            return;
        }

        await PersistAsync(_journal.UndoAsync, () => _notice.Text = "Last answer disregarded; its persisted retraction excludes it from training.")
            .ConfigureAwait(true);
        if (!IsDisposed) { _undo.Checked = false; }
    }

    private async Task<bool> PersistAsync(Func<Task> write, Action success)
    {
        _busy = true;
        _answers.Enabled = false;
        _undo.Enabled = false;
        _pause.Enabled = false;
        try
        {
            _pendingWrite = write();
            await _pendingWrite.ConfigureAwait(true);
            success();
            return true;
        }
        catch (Exception exception)
        {
            _notice.Text = $"NOT SAVED: {exception.Message}";
            _reportError("Road learning answer was not saved; retry before the question expires", exception);
            return false;
        }
        finally
        {
            _pendingWrite = null;
            _busy = false;
            _answers.Enabled = _question is not null;
            _undo.Enabled = _journal.CanUndo;
            _pause.Enabled = true;
            if (_ending)
            {
                Close();
            }
        }
    }

    internal void DismissQuestion(string reason)
    {
        _question = null;
        _answers.Enabled = false;
        _confidence.Text = "My guess - Confidence -- %:";
        _condition.Text = reason;
        _notice.Text = "Live sensing continues. No label is inferred from dismissal or silence.";
        foreach (ToolStripItem item in _sources.Items)
        {
            item.Text = "--";
            item.ToolTipText = "";
        }
    }

    private void ShowEvidence()
    {
        if (_busy || _question is not { } question)
        {
            return;
        }

        DismissQuestion("Evidence opened; question dismissed as stale.");
        _detailsOpen = true;
        try
        {
            using Form details = new()
            {
                Text = "Frozen guess evidence - question dismissed",
                Size = new Size(1150, 480),
                StartPosition = FormStartPosition.CenterParent,
                Font = Font,
                AutoScaleMode = AutoScaleMode.Dpi
            };
            DataGridView table = new()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            foreach (string column in new[] { "Source", "Sensors", "Samples", "Speed m/s", "Accel m/s2", "RPM",
                "Vibration g SD", "Pitch deg", "Gyro deg/s", "Evidence %", "Reason" })
            {
                table.Columns.Add(column, column);
            }

            foreach (RoadSourceEvidence source in question.Sources)
            {
                table.Rows.Add(source.Abbreviation, source.Sensors, source.Samples, Format(source.Speed),
                    Format(source.Acceleration), Format(source.Rpm), Format(source.Vibration), Format(source.Pitch),
                    Format(source.AngularRate), $"{source.Contribution:0.0}", source.Explanation);
            }

            Label heading = new()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                Text = $"{RoadConditions.Headline(question.Condition, question.Level)}; {question.Start:T} - {question.End:T}\r\n"
                    + "Percentages are normalized rule-evidence shares, not independent probability gains. RPM and gyro are context only."
            };
            details.Controls.Add(table);
            details.Controls.Add(heading);
            details.ShowDialog(this);
        }
        finally
        {
            _detailsOpen = false;
        }
    }

    private void UpdatePause()
    {
        _pause.Text = $"Pause {_pauseMinutes} min.";
        _less.Enabled = _pauseMinutes > 1;
        _more.Enabled = _pauseMinutes < 20;
    }

    internal async Task FinishAsync()
    {
        _session.End();
        _ending = true;
        if (_pendingWrite is { } pending)
        {
            try
            {
                await pending.ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                _reportError("Road learning ended with an unsaved answer", exception);
            }
        }

        if (!IsDisposed) { Close(); }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _session.End();
        _ending = true;
        if (_busy) { e.Cancel = true; }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.End();
            _timer.Dispose();
            _headingFont.Dispose();
            _answerFont.Dispose();
            _controlFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private static string Format(double? number)
        => number?.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture) ?? "--";
}
