using ImGuiNET;
using System.Runtime.CompilerServices;

namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// Centralized runtime error and warning overlay for diagnostics and failure tracking.
/// </summary>
public static class ErrorListWindow
{
    private static readonly Dictionary<string, ErrorItem> _errors = [];
    private static readonly object _lock = new();
    private static UI? _ui;

    public static bool Show { get; set; }

    /// <summary>
    /// Registers an error or warning message with caller tracking and optional auto-dismiss duration.
    /// </summary>
    public static void Add(
        string error,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        float showTimeSeconds = 0)
    {
        lock (_lock)
        {
            if (!_errors.TryGetValue(error, out var item))
            {
                string fileName = Path.GetFileName(filePath);
                item = new ErrorItem
                {
                    MaxTime = showTimeSeconds,
                    CallerInfo = $"[{fileName}:{lineNumber}] ({memberName})"
                };
                _errors.Add(error, item);
            }

            item.Count++;
            item.CurrentTime = 0;
            Show = true;
        }
    }

    internal static void SetUI(UI ui)
    {
        _ui = ui;
    }

    internal static void Update(float deltaTime)
    {
        lock (_lock)
        {
            foreach (var item in _errors.Values)
            {
                item.CurrentTime += deltaTime;
            }
        }
    }

    internal static void Render()
    {
        if (!Show)
            return;

        _ui?.SetFontSize(15);
        if (ImGui.Begin("Error List", ImGuiWindowFlags.AlwaysAutoResize))
        {
            lock (_lock)
            {
                foreach (var (msg, item) in _errors)
                {
                    if (item.MaxTime > 0 && item.CurrentTime > item.MaxTime)
                        continue;

                    string count = item.Count > 1 ? $"({item.Count}) " : "";
                    ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f), $"{count}{msg}");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(item.CallerInfo);
                    }
                }
            }
        }
        ImGui.End();
    }
}
