namespace Phoenix.Framework.AssetImport.Tracking;

/// <summary>
/// Represents a tracked asset loading or processing operation.
/// </summary>
public sealed class AssetLoadOperation
{
    private readonly object _lock = new();

    public string Id { get; }
    public string AssetPath { get; }
    public string DisplayName { get; }
    public AssetLoadOperationType Type { get; }
    public string Status { get; private set; }
    public float Progress { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool HasFailed { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public string AssetName => DisplayName;
    public string StatusMessage => Status;
    public bool IsFailed => HasFailed;
    public float ElapsedSeconds => (float)Elapsed.TotalSeconds;

    public TimeSpan Elapsed
    {
        get
        {
            DateTime end = EndTime ?? DateTime.UtcNow;
            return end - StartTime;
        }
    }

    public AssetLoadOperation(string id, string assetPath, AssetLoadOperationType type, string initialStatus)
    {
        Id = id;
        AssetPath = assetPath;
        DisplayName = Path.GetFileName(assetPath);
        Type = type;
        Status = initialStatus;
        Progress = -1f;
        StartTime = DateTime.UtcNow;
    }

    public void UpdateStatus(string status, float progress = -1f)
    {
        lock (_lock)
        {
            Status = status;
            Progress = progress;
        }
    }

    public void Complete(string finalStatus = "Completed")
    {
        lock (_lock)
        {
            if (IsCompleted)
                return;

            Status = finalStatus;
            Progress = 1.0f;
            IsCompleted = true;
            EndTime = DateTime.UtcNow;
        }

        AssetLoadingTracker.NotifyOperationCompleted(this);
    }

    public void Fail(string errorMessage)
    {
        lock (_lock)
        {
            if (IsCompleted)
                return;

            Status = $"Failed: {errorMessage}";
            ErrorMessage = errorMessage;
            HasFailed = true;
            IsCompleted = true;
            EndTime = DateTime.UtcNow;
        }

        AssetLoadingTracker.NotifyOperationFailed(this);
    }
}
