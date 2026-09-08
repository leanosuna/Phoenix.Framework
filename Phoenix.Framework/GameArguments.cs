using System.Globalization;
using Phoenix.Framework.Rendering.Windowing;
using Silk.NET.Maths;

namespace Phoenix.Framework;

/// <summary>
/// Parses, stores, and exposes command-line arguments for engine configuration and game logic.
/// </summary>
public sealed class GameArguments
{
    private static readonly HashSet<string> s_knownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "x11",
        "force-x11",
        "wayland",
        "force-wayland",
        "vsync",
        "no-vsync",
        "fullscreen",
        "windowed",
        "vulkan-validation",
        "debug-layers",
        "verbose",
        "debug"
    };

    private static readonly HashSet<string> s_knownValueOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "platform",
        "width",
        "w",
        "height",
        "h",
        "resolution",
        "res",
        "title",
        "fps",
        "fps-limit",
        "max-fps",
        "clear-color",
        "log-level"
    };

    private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _positionals = [];
    private readonly string[] _rawArgs;

    /// <summary>
    /// Gets the raw array of command-line arguments passed to the application.
    /// </summary>
    public IReadOnlyList<string> RawArgs => _rawArgs;

    /// <summary>
    /// Gets all parsed options formatted as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string> Options => _options;

    /// <summary>
    /// Gets the set of present boolean flags and switches.
    /// </summary>
    public IReadOnlySet<string> Flags => _flags;

    /// <summary>
    /// Gets all positional arguments not associated with any option or flag.
    /// </summary>
    public IReadOnlyList<string> Positionals => _positionals;

    /// <summary>
    /// Gets the requested windowing platform backend.
    /// </summary>
    public WindowBackend WindowBackend { get; private set; } = WindowBackend.Auto;

    /// <summary>
    /// Gets the requested vertical synchronization mode, or null if not explicitly set via command-line arguments.
    /// </summary>
    public bool? VSync { get; private set; }

    /// <summary>
    /// Gets the requested window width in pixels, or null if not specified.
    /// </summary>
    public int? Width { get; private set; }

    /// <summary>
    /// Gets the requested window height in pixels, or null if not specified.
    /// </summary>
    public int? Height { get; private set; }

    /// <summary>
    /// Gets whether fullscreen mode was requested, or null if not specified.
    /// </summary>
    public bool? Fullscreen { get; private set; }

    /// <summary>
    /// Gets the requested window title, or null if not specified.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Gets the requested target frames-per-second limit, or null if not specified.
    /// </summary>
    public int? FpsLimit { get; private set; }

    /// <summary>
    /// Gets whether Vulkan validation layers were explicitly enabled via arguments.
    /// </summary>
    public bool VulkanValidation { get; private set; }

    /// <summary>
    /// Gets whether verbose diagnostic logging was requested via arguments.
    /// </summary>
    public bool Verbose { get; private set; }

    private GameArguments(string[]? args)
    {
        _rawArgs = args ?? [];
        ParseInternal();
        ExtractFrameworkSettings();
    }

    /// <summary>
    /// Parses an array of command-line argument tokens into a GameArguments instance.
    /// If null is provided, retrieves arguments from Environment.GetCommandLineArgs().
    /// </summary>
    public static GameArguments Parse(string[]? args = null)
    {
        args ??= Environment.GetCommandLineArgs();
        return new GameArguments(args);
    }

    /// <summary>
    /// Checks whether a flag or option with the specified name was provided.
    /// Accepts names with or without leading dashes.
    /// </summary>
    public bool Has(string name)
    {
        var normalized = NormalizeKey(name);
        return _flags.Contains(normalized) || _options.ContainsKey(normalized);
    }

    /// <summary>
    /// Checks whether a boolean switch with the specified name was provided.
    /// Accepts names with or without leading dashes.
    /// </summary>
    public bool HasFlag(string name) => Has(name);

    /// <summary>
    /// Returns the string value for a given option, or null if the option was not specified.
    /// </summary>
    public string? Get(string name)
    {
        var normalized = NormalizeKey(name);
        return _options.TryGetValue(normalized, out var value) ? value : null;
    }

    /// <summary>
    /// Returns the string value for a given option, or the specified default value if not found.
    /// </summary>
    public string GetString(string name, string defaultValue = "")
    {
        return Get(name) ?? defaultValue;
    }

    /// <summary>
    /// Parses and returns the typed value of an option, or returns defaultValue if not present or invalid.
    /// If T is boolean, also returns true if the name was passed as a standalone flag.
    /// </summary>
    public T Get<T>(string name, T defaultValue = default!)
    {
        var normalized = NormalizeKey(name);

        if (_options.TryGetValue(normalized, out var strVal))
        {
            try
            {
                if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(strVal, out var bVal))
                        return (T)(object)bVal;
                    if (strVal is "1" or "yes" or "on" or "true")
                        return (T)(object)true;
                    if (strVal is "0" or "no" or "off" or "false")
                        return (T)(object)false;
                }

                return (T)Convert.ChangeType(strVal, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        if (typeof(T) == typeof(bool) && _flags.Contains(normalized))
        {
            return (T)(object)true;
        }

        return defaultValue;
    }

    /// <summary>
    /// Iterates through raw arguments and categorizes tokens into options, flags, and positionals.
    /// </summary>
    private void ParseInternal()
    {
        for (int i = 0; i < _rawArgs.Length; i++)
        {
            var token = _rawArgs[i];
            if (string.IsNullOrWhiteSpace(token))
                continue;

            if (!token.StartsWith('-'))
            {
                _positionals.Add(token);
                continue;
            }

            int equalIndex = token.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = NormalizeKey(token[..equalIndex]);
                var val = token[(equalIndex + 1)..].Trim('"', '\'');
                _options[key] = val;
                _flags.Add(key);
                continue;
            }

            var flagKey = NormalizeKey(token);

            if (s_knownFlags.Contains(flagKey))
            {
                _flags.Add(flagKey);
                _options[flagKey] = "true";
                continue;
            }

            if (i + 1 < _rawArgs.Length && !_rawArgs[i + 1].StartsWith('-'))
            {
                var nextVal = _rawArgs[i + 1].Trim('"', '\'');
                _options[flagKey] = nextVal;
                _flags.Add(flagKey);
                i++;
                continue;
            }

            _flags.Add(flagKey);
            _options[flagKey] = "true";
        }
    }

    /// <summary>
    /// Populates strongly-typed framework properties from parsed options and environment variables.
    /// </summary>
    private void ExtractFrameworkSettings()
    {
        ExtractPlatform();
        ExtractVSync();
        ExtractDimensions();
        ExtractWindowMode();

        if (_options.TryGetValue("title", out var title))
            Title = title;

        if (TryGetInt("fps", out var fps) || TryGetInt("fps-limit", out fps) || TryGetInt("max-fps", out fps))
            FpsLimit = fps;

        VulkanValidation = Has("vulkan-validation") || Has("debug-layers");
        Verbose = Has("verbose");
    }

    /// <summary>
    /// Resolves the windowing platform backend from arguments and environment variables.
    /// </summary>
    private void ExtractPlatform()
    {
        if (Has("x11") || Has("force-x11"))
        {
            WindowBackend = WindowBackend.X11;
            return;
        }

        if (Has("wayland") || Has("force-wayland"))
        {
            WindowBackend = WindowBackend.Wayland;
            return;
        }

        if (_options.TryGetValue("platform", out var platVal))
        {
            switch (platVal.ToLowerInvariant())
            {
                case "x11":
                    WindowBackend = WindowBackend.X11;
                    return;
                case "wayland":
                    WindowBackend = WindowBackend.Wayland;
                    return;
            }
        }

        var env = Environment.GetEnvironmentVariable("PHOENIX_PLATFORM")?.ToLowerInvariant();
        switch (env)
        {
            case "x11":
                WindowBackend = WindowBackend.X11;
                return;
            case "wayland":
                WindowBackend = WindowBackend.Wayland;
                return;
            default:
                WindowBackend = WindowBackend.Auto;
                break;
        }
    }

    /// <summary>
    /// Resolves vertical synchronization preference from arguments.
    /// </summary>
    private void ExtractVSync()
    {
        if (Has("no-vsync"))
        {
            VSync = false;
            return;
        }

        if (_options.TryGetValue("vsync", out var vsyncVal))
        {
            if (bool.TryParse(vsyncVal, out var bVal))
            {
                VSync = bVal;
                return;
            }

            switch (vsyncVal.ToLowerInvariant())
            {
                case "0" or "false" or "off" or "no":
                    VSync = false;
                    return;
                case "1" or "true" or "on" or "yes":
                    VSync = true;
                    return;
            }
        }

        if (_flags.Contains("vsync"))
        {
            VSync = true;
        }
    }

    /// <summary>
    /// Resolves window dimensions from width, height, or resolution options.
    /// </summary>
    private void ExtractDimensions()
    {
        if (TryGetInt("width", out var w) || TryGetInt("w", out w))
            Width = w;

        if (TryGetInt("height", out var h) || TryGetInt("h", out h))
            Height = h;

        var res = Get("resolution") ?? Get("res");
        if (!string.IsNullOrEmpty(res))
        {
            var parts = res.Split(['x', 'X', '*']);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], CultureInfo.InvariantCulture, out var rw) &&
                int.TryParse(parts[1], CultureInfo.InvariantCulture, out var rh))
            {
                Width = rw;
                Height = rh;
            }
        }
    }

    /// <summary>
    /// Resolves fullscreen or windowed preferences.
    /// </summary>
    private void ExtractWindowMode()
    {
        if (Has("fullscreen"))
        {
            Fullscreen = true;
            return;
        }

        if (Has("windowed"))
        {
            Fullscreen = false;
        }
    }

    /// <summary>
    /// Attempts to parse an integer option value using invariant culture.
    /// </summary>
    private bool TryGetInt(string name, out int value)
    {
        if (_options.TryGetValue(name, out var str) && int.TryParse(str, CultureInfo.InvariantCulture, out value))
            return true;

        value = 0;
        return false;
    }

    /// <summary>
    /// Strips leading dashes and returns a lowercased option key.
    /// </summary>
    private static string NormalizeKey(string key) => key.TrimStart('-').ToLowerInvariant();
}
