using ImGuiNET;
using Phoenix.Framework.AssetImport.Tracking;
using System.Numerics;

namespace Phoenix.Framework.Rendering.GUI;

/// <summary>
/// ImGui debug overlay displaying active and recent background asset loading tasks, status messages, and overall progress.
/// </summary>
public static class AssetLoadingDebugWindow
{
    private static bool _wasLoading;

    public static bool Show { get; set; } = true;
    public static bool AutoShowOnLoading { get; set; } = true;

    /// <summary>
    /// Updates window state based on active background tasks.
    /// </summary>
    internal static void Update(float deltaTime)
    {
        bool isLoading = AssetLoadingTracker.IsLoading;
        if (AutoShowOnLoading && isLoading && !_wasLoading)
        {
            Show = true;
        }
        _wasLoading = isLoading;
    }

    /// <summary>
    /// Renders the asset loading monitor window if visible.
    /// </summary>
    internal static void Render()
    {
        if (!Show)
            return;

        ImGui.SetNextWindowSize(new Vector2(620, 360), ImGuiCond.FirstUseEver);

        bool show = Show;
        if (ImGui.Begin("Asset Loading Monitor", ref show, ImGuiWindowFlags.NoCollapse))
        {
            Show = show;

            float overall = AssetLoadingTracker.OverallProgress;
            int activeCount = AssetLoadingTracker.ActiveCount;
            int completedCount = AssetLoadingTracker.CompletedCount;

            string progressText = activeCount > 0
                ? $"{overall * 100:F0}% ({activeCount} active, {completedCount} finished)"
                : "Idle (All assets loaded)";

            ImGui.TextUnformatted("Overall Progress:");
            ImGui.ProgressBar(overall, new Vector2(-1, 22), progressText);
            ImGui.Spacing();

            var active = AssetLoadingTracker.ActiveOperations;
            ImGui.TextUnformatted($"Active Operations ({active.Count}):");

            if (active.Count == 0)
            {
                ImGui.TextDisabled("No active background asset loading operations.");
            }
            else
            {
                if (ImGui.BeginTable("ActiveTasksTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new Vector2(0, 150)))
                {
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 65);
                    ImGui.TableSetupColumn("Asset", ImGuiTableColumnFlags.WidthStretch, 1.2f);
                    ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthStretch, 1.5f);
                    ImGui.TableSetupColumn("Progress", ImGuiTableColumnFlags.WidthFixed, 90);
                    ImGui.TableSetupColumn("Elapsed", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < active.Count; i++)
                    {
                        var op = active[i];
                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);
                        Vector4 typeColor = op.Type switch
                        {
                            AssetLoadOperationType.Model => new Vector4(0.3f, 0.7f, 1.0f, 1.0f),
                            AssetLoadOperationType.Texture => new Vector4(0.4f, 1.0f, 0.5f, 1.0f),
                            AssetLoadOperationType.Shader => new Vector4(1.0f, 0.8f, 0.2f, 1.0f),
                            AssetLoadOperationType.Audio => new Vector4(1.0f, 0.4f, 0.7f, 1.0f),
                            _ => new Vector4(0.8f, 0.8f, 0.8f, 1.0f)
                        };
                        ImGui.TextColored(typeColor, op.Type.ToString());

                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextUnformatted(op.AssetName);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(op.AssetPath);

                        ImGui.TableSetColumnIndex(2);
                        ImGui.TextUnformatted(op.StatusMessage);

                        ImGui.TableSetColumnIndex(3);
                        ImGui.ProgressBar(op.Progress, new Vector2(-1, 15), $"{op.Progress * 100:F0}%");

                        ImGui.TableSetColumnIndex(4);
                        ImGui.TextUnformatted($"{op.ElapsedSeconds:F1}s");
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var recent = AssetLoadingTracker.RecentOperations;
            if (ImGui.CollapsingHeader($"Recent Completed Operations ({recent.Count})", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (recent.Count == 0)
                {
                    ImGui.TextDisabled("No completed operations yet.");
                }
                else
                {
                    if (ImGui.BeginTable("RecentTasksTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 100)))
                    {
                        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 65);
                        ImGui.TableSetupColumn("Asset", ImGuiTableColumnFlags.WidthStretch, 1.2f);
                        ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthStretch, 1.5f);
                        ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 60);
                        ImGui.TableHeadersRow();

                        for (int i = 0; i < recent.Count; i++)
                        {
                            var op = recent[i];
                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.TextDisabled(op.Type.ToString());

                            ImGui.TableSetColumnIndex(1);
                            ImGui.TextUnformatted(op.AssetName);
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(op.AssetPath);

                            ImGui.TableSetColumnIndex(2);
                            if (op.IsFailed)
                                ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), op.ErrorMessage ?? "Failed");
                            else
                                ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.4f, 1.0f), op.StatusMessage);

                            ImGui.TableSetColumnIndex(3);
                            ImGui.TextUnformatted($"{op.ElapsedSeconds:F2}s");
                        }

                        ImGui.EndTable();
                    }
                }
            }
        }
        ImGui.End();
    }
}
