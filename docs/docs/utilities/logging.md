# Logging and metrics

Namespace: `Phoenix.Framework.FileHelpers`

Phoenix provides file and console logging via `Log`, an in-game error overlay via `ErrorListWindow`, and frame performance counters via `Metrics`.

---

## Log (file and console)

Thread-safe logging writing to `{BaseDirectory}/LOG.txt` and optionally to the console.

### Configuration

```csharp
Log.Enabled = true;      // Enable logging
Log.Verbose = true;      // Include verbose messages
Log.ConsoleWrite = true; // Output to standard console
Log.Date = true;         // Include date timestamp
Log.Time = true;         // Include time timestamp
```

### Log levels

```csharp
Log.Info("Game started (Vulkan 1.3 pipeline)");
Log.Warn("Low GPU memory warning");
Log.Error("Failed to find model asset: 3D/missing.fbx");
Log.Debug("Frame 60 completed");
Log.Exception("Fatal error encountered");
```

### Clearing the log

```csharp
Log.ClearLog();
```

---

## ErrorListWindow (in-game overlay)

`ErrorListWindow` collects and displays warnings and non-fatal runtime errors as an ImGui overlay:

```csharp
// Simple error
ErrorListWindow.Add("Asset format unrecognized");

// Auto-expiring alert (hidden after 5 seconds)
ErrorListWindow.Add("Device resumed from standby", showTimeSeconds: 5f);

// Toggle visibility
ErrorListWindow.Show = true;
```

Deduplication: adding the same error string increments its counter rather than creating duplicate lines.

---

## Performance metrics (Metrics)

Exposed as `PhoenixGame.Metrics`. Tracks frame timings and update rates:

| Counter | Type | Description |
|---|---|---|
| `Time` | `double` | Total elapsed simulation time in seconds |
| `FrameTime` | `double` | Elapsed delta time of the most recent render frame |
| `FT_SAMPLE` | `int` | Smoothed frame time in milliseconds |
| `FPS` | `double` | Instantaneous frames per second |
| `FPS_SAMPLE` | `int` | Smoothed frames per second counter |
| `UPS` | `double` | Instantaneous updates per second |
| `UPS_SAMPLE` | `int` | Smoothed updates per second counter |

Display in your UI or ImGui overlay:

```csharp
protected override void RenderUI()
{
    UI.DrawText($"FPS: {Metrics.FPS_SAMPLE}", new Vector2(10, 10), Vector4.One, 16);
    UI.DrawText($"FT:  {Metrics.FT_SAMPLE}ms", new Vector2(10, 30), Vector4.One, 16);
}
```
