using System.Diagnostics;

namespace Random_File_Select;

/// <summary>How a folder load ended.</summary>
public enum FolderLoadStatus
{
    Completed,
    Cancelled,
    Faulted
}

/// <summary>Typed result of a modal load: scan result on success, or the exception on failure; elapsed is the dialog's own time.</summary>
public sealed record FolderLoadOutcome(
    FolderLoadStatus Status,
    FolderScanResult? Result,
    Exception? Error,
    TimeSpan Elapsed);

/// <summary>Modal, non-dismissible progress dialog that runs a folder scan off the UI thread and closes itself when finished.</summary>
public sealed class FolderLoadProgressDialog : Form
{
    private readonly Func<CancellationToken, Action<int, string>, FolderScanResult> _scan;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 50 };
    private readonly Label _countLabel = new();
    private readonly Label _pathLabel = new();
    private readonly Button _cancelButton = new();
    private bool _isLoading;
    private volatile int _processedCount;
    private volatile string _currentPath = string.Empty;

    /// <summary>Creates the dialog around a scan delegate that receives a cancellation token and a progress callback.</summary>
    public FolderLoadProgressDialog(Func<CancellationToken, Action<int, string>, FolderScanResult> scan)
    {
        _scan = scan;
        BuildLayout();

        _refreshTimer.Tick += (_, _) => RefreshLabels();
        Shown += OnShown;
        FormClosing += OnFormClosing;
    }

    /// <summary>Outcome recorded when the dialog closes; null until the scan finishes.</summary>
    public FolderLoadOutcome? Outcome { get; private set; }

    /// <summary>Creates the marquee bar, labels, and Cancel button in a fixed-size, control-box-free dialog.</summary>
    private void BuildLayout()
    {
        Text = "Loading folders";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 130);

        _countLabel.Text = "Folders processed: 0";
        _countLabel.SetBounds(12, 12, 436, 18);

        _pathLabel.AutoEllipsis = true;
        _pathLabel.SetBounds(12, 34, 436, 18);

        var bar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };
        bar.SetBounds(12, 58, 436, 18);

        _cancelButton.Text = "Cancel";
        _cancelButton.SetBounds(373, 90, 75, 28);
        _cancelButton.Click += (_, _) => RequestCancel();

        Controls.AddRange([_countLabel, _pathLabel, bar, _cancelButton]);
    }

    /// <summary>Requests cancellation and disables Cancel; the dialog stays open until the scan task actually ends.</summary>
    private void RequestCancel()
    {
        _cancelButton.Enabled = false;
        _cancelButton.Text = "Cancelling...";
        _cancellation.Cancel();
    }

    /// <summary>Stores the latest count and path from the worker thread (no per-directory UI marshaling).</summary>
    private void Report(int processed, string path)
    {
        _currentPath = path;
        _processedCount = processed;
    }

    /// <summary>Copies the latest worker-reported values into the labels.</summary>
    private void RefreshLabels()
    {
        _countLabel.Text = $"Folders processed: {_processedCount:N0}";
        _pathLabel.Text = _currentPath;
    }

    /// <summary>Runs the scan on a worker thread, records the outcome, then clears the loading flag and closes.</summary>
    private async void OnShown(object? sender, EventArgs e)
    {
        _isLoading = true;
        _refreshTimer.Start();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            FolderScanResult result = await Task.Run(() => _scan(_cancellation.Token, Report));
            Outcome = new FolderLoadOutcome(FolderLoadStatus.Completed, result, null, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            Outcome = new FolderLoadOutcome(FolderLoadStatus.Cancelled, null, null, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            Outcome = new FolderLoadOutcome(FolderLoadStatus.Faulted, null, ex, stopwatch.Elapsed);
        }
        finally
        {
            _refreshTimer.Stop();
            RefreshLabels();
            _isLoading = false;
            Close();
        }
    }

    /// <summary>Blocks every close attempt (X, Alt+F4, system) while the scan is still running.</summary>
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isLoading)
            e.Cancel = true;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
            _cancellation.Dispose();
        }

        base.Dispose(disposing);
    }
}
