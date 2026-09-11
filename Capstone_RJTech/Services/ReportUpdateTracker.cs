namespace Capstone_RJTech.Services;

public sealed class ReportUpdateTracker
{
    private readonly object _signalLock = new();
    private long _sourceVersion;
    private long _refreshedVersion = -1;
    private TaskCompletionSource<long> _changeSignal = NewSignal();

    public long SourceVersion => Interlocked.Read(ref _sourceVersion);
    public long RefreshedVersion => Interlocked.Read(ref _refreshedVersion);
    public bool NeedsRefresh => SourceVersion != RefreshedVersion;

    public void MarkSourceChanged()
    {
        TaskCompletionSource<long> signal;
        long version;

        lock (_signalLock)
        {
            version = Interlocked.Increment(ref _sourceVersion);
            signal = _changeSignal;
            _changeSignal = NewSignal();
        }

        signal.TrySetResult(version);
    }

    public void MarkRefreshed(long sourceVersion)
        => Interlocked.Exchange(ref _refreshedVersion, sourceVersion);

    public async Task<long> WaitForSourceChangeAsync(
        long knownVersion,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = SourceVersion;
        if (currentVersion != knownVersion)
            return currentVersion;

        Task<long> changeTask;
        lock (_signalLock)
        {
            changeTask = _changeSignal.Task;
            currentVersion = SourceVersion;
        }

        if (currentVersion != knownVersion)
            return currentVersion;

        var timeoutTask = Task.Delay(timeout, cancellationToken);
        var completedTask = await Task.WhenAny(changeTask, timeoutTask);
        cancellationToken.ThrowIfCancellationRequested();
        return completedTask == changeTask ? await changeTask : SourceVersion;
    }

    private static TaskCompletionSource<long> NewSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
