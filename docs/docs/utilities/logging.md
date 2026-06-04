# Logging

Phoenix provides two logging mechanisms: `Log` (file-based) and `ErrorListWindow` (in-game overlay).

## Log (File-Based)

Thread-safe file logging to `{BaseDirectory}/LOG.txt`.

### Configuration

```csharp
Log.Enabled = true;      // Enable logging
Log.Verbose = true;      // Include verbose messages
Log.ConsoleWrite = true; // Also write to console
Log.Date = true;         // Include date in log entries
Log.Time = true;         // Include time in log entries
```

### Log Levels

```csharp
// Informational
Log.Info("Game started");
Log.Info("Loaded {0} models", modelCount);

// Warnings
Log.Warn("Low disk space, may fail to save");

// Errors
Log.Error("Failed to load texture: textures/missing.png");

// Debug
Log.Debug("Processing frame {0}", frameCount);

// Exceptions
Log.Exception("Crash occurred", ex);
```

### Clearing the Log

```csharp
Log.ClearLog();
```

### Auto-Enable on Exception

When `PhoenixGame.Run()` catches an unhandled exception, it automatically enables logging with all flags and writes the exception to the log file.

```
// LOG.txt (example):
[2026-05-10 14:32:01] ERROR: Failed to load shader: shaders/pbr
   at GLShader.LoadShader(ShaderType type, string path)
   at AssetLoader.LoadShader(string name)
```

### Thread Safety

Log writes are protected by a `lock` statement. However, `File.AppendAllText` opens and closes the file per-write, which may cause issues in multi-process scenarios.

## ErrorListWindow (In-Game Overlay)

Centralized error reporting displayed as an ImGui overlay. Errors from all subsystems are collected here.

### Visibility

```csharp
ErrorListWindow.Show = true;   // Show overlay (default: false)
ErrorListWindow.Show = false;  // Hide overlay
```

### Adding Errors

Errors are auto-added by the framework. You can also add custom errors:

```csharp
// Simple error — caller info auto-filled
ErrorListWindow.Add("Something went wrong");

// With auto-expire (hidden after N seconds)
ErrorListWindow.Add("Temporary issue", showTimeSeconds: 5f);

// With explicit caller info
ErrorListWindow.Add(
    "Custom error message",
    "MyGame.cs",       // filePath
    42,                 // lineNumber
    "MyMethod",         // memberName
    0                   // showTimeSeconds (0 = persistent)
);
```

### How Errors Are Deduplicated

Adding the same error message twice increments the count instead of creating a new entry:

```csharp
ErrorListWindow.Add("Shader compile failed");
ErrorListWindow.Add("Shader compile failed");
// Shows: "Shader compile failed" ×2
```

### Error Display

Each error entry shows:
- **Error icon and message**
- **Count** (if deduplicated)
- **Hover tooltip** showing caller file, line number, and method name

### Which Systems Report Errors

| System | When It Reports |
|--------|----------------|
| `GLShader` | Uniform not found (unless `ignoreUniformsNotFound`) |
| `GLTexture` | Texture loading failures |
| `SoundManager` | OpenAL device/context errors |
| `Graphics` | State management errors |
| `AssetLoader` | Asset not found in manifest |
| `GLCompiler` | Shader compile/link failures |

## Complete Example

```csharp
public class MyGame : PhoenixGame
{
    protected override void Initialize()
    {
        // Enable file logging
        Log.Enabled = true;
        Log.Verbose = true;
        Log.Date = true;
        Log.Time = true;

        // Show error overlay in debug builds
#if DEBUG
        ErrorListWindow.Show = true;
#endif

        Log.Info("Game initialized");
    }

    protected override void Update(double dt)
    {
        try
        {
            // Game logic
        }
        catch (Exception ex)
        {
            Log.Exception("Update loop error", ex);
            ErrorListWindow.Add($"Update error: {ex.Message}");
        }
    }

    protected override void Render(double dt)
    {
        try
        {
            // Rendering
        }
        catch (Exception ex)
        {
            Log.Exception("Render loop error", ex);
            ErrorListWindow.Add($"Render error: {ex.Message}");
        }
    }

    protected override void OnClose()
    {
        Log.Info("Game closing");
        Log.ClearLog();
        base.OnClose();
    }
}
```

## Log File Format

Each log line format:

```
[{Date} {Time}] {Level}: {Message}
```

Example:

```
[2026-05-10 14:32:01] INFO: Game started
[2026-05-10 14:32:02] WARN: Low disk space
[2026-05-10 14:32:05] ERROR: Shader compile failed
[2026-05-10 14:32:05] EXCEPTION: System.Exception: Shader compile failed
```

## See Also

- [ErrorListWindow](../rendering/ui.md) — the overlay component
- [Shaders](../rendering/shaders.md) — shader uniform errors
- [AssetLoader](../asset-pipeline/loading.md) — asset loading errors
