namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// Represents an entry in the centralized error reporting overlay.
/// </summary>
internal sealed class ErrorItem
{
    public int Count { get; set; }
    public float CurrentTime { get; set; }
    public float MaxTime { get; set; }
    public string CallerInfo { get; set; } = string.Empty;
}
