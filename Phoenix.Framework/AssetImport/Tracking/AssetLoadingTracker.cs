using System.Collections.Concurrent;

namespace Phoenix.Framework.AssetImport.Tracking;

/// <summary>
/// Central thread-safe tracking hub for monitoring background asset loading and compression progress.
/// </summary>
public static class AssetLoadingTracker
{
    private static readonly ConcurrentDictionary<string, AssetLoadOperation> _activeOperations = new();
    private static readonly ConcurrentQueue<AssetLoadOperation> _recentOperations = new();
    private static readonly object _statsLock = new();
    private static int _totalOperations;
    private static int _completedOperations;
    private const int MaxRecentHistory = 30;

    public static bool IsLoading => !_activeOperations.IsEmpty;
    public static int ActiveCount => _activeOperations.Count;
    public static int CompletedCount => _completedOperations;
    public static int TotalCount => _totalOperations;

    public static IReadOnlyList<AssetLoadOperation> ActiveOperations => GetActiveOperations();
    public static IReadOnlyList<AssetLoadOperation> RecentOperations => GetRecentOperations();

    public static float OverallProgress
    {
        get
        {
            lock (_statsLock)
            {
                if (_totalOperations == 0)
                    return 1.0f;

                return Math.Clamp((float)_completedOperations / _totalOperations, 0.0f, 1.0f);
            }
        }
    }

    public static IReadOnlyList<AssetLoadOperation> GetActiveOperations()
    {
        return _activeOperations.Values.ToList();
    }

    public static IReadOnlyList<AssetLoadOperation> GetRecentOperations()
    {
        return _recentOperations.ToArray();
    }

    public static AssetLoadOperation BeginOperation(string assetPath, AssetLoadOperationType type, string initialStatus)
    {
        string id = $"{assetPath}_{Guid.NewGuid():N}";
        AssetLoadOperation op = new(id, assetPath, type, initialStatus);

        lock (_statsLock)
        {
            _totalOperations++;
        }

        _activeOperations[id] = op;
        return op;
    }

    public static void UpdateOperation(string id, string status, float progress = -1f)
    {
        if (_activeOperations.TryGetValue(id, out var op))
        {
            op.UpdateStatus(status, progress);
        }
    }

    public static void CompleteOperation(string id, string finalStatus = "Completed")
    {
        if (_activeOperations.TryRemove(id, out var op))
        {
            op.Complete(finalStatus);

            lock (_statsLock)
            {
                _completedOperations++;
            }

            _recentOperations.Enqueue(op);
            while (_recentOperations.Count > MaxRecentHistory && _recentOperations.TryDequeue(out _))
            {
            }
        }
    }

    public static void FailOperation(string id, string errorMessage)
    {
        if (_activeOperations.TryRemove(id, out var op))
        {
            op.Fail(errorMessage);

            lock (_statsLock)
            {
                _completedOperations++;
            }

            _recentOperations.Enqueue(op);
            while (_recentOperations.Count > MaxRecentHistory && _recentOperations.TryDequeue(out _))
            {
            }
        }
    }

    public static void ResetStats()
    {
        lock (_statsLock)
        {
            if (_activeOperations.IsEmpty)
            {
                _totalOperations = 0;
                _completedOperations = 0;
            }
        }
    }
}
