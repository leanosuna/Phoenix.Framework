# Input

Namespace: `Phoenix.Framework.Inputs`

The `Input` class handles keyboard and mouse input via Silk.NET. Supports any number of connected keyboards and mice. Exposed as `PhoenixGame.Input`.

## Properties

| Name | Type | Description |
|---|---|---|
| `MouseSensitivity` | `float` | Mouse delta multiplier (default `0.001f`) |
| `MouseDelta` | `Vector2` | Accumulated mouse movement this frame, multiplied by sensitivity |
| `MouseWheelValue` | `float` | Accumulated scroll wheel delta this frame (reset after read) |

## Methods

### Keyboard

| Method | Return | Description |
|---|---|---|
| `KeyDown(Key key)` | `bool` | `true` while the key is held |
| `KeyDownOnce(Key key)` | `bool` | `true` only on the frame the key is first pressed |

```csharp
if (Input.KeyDown(Key.W))
    MoveForward(speed * (float)dt);

if (Input.KeyDownOnce(Key.E))
    Interact();
```

### Mouse

| Method | Return | Description |
|---|---|---|
| `MouseDown(MouseButton button)` | `bool` | `true` while the button is held |
| `MouseDownOnce(MouseButton button)` | `bool` | `true` only on the frame the button is first pressed |
| `MouseLeftDown()` | `bool` | Shorthand for `MouseDown(Left)` |
| `MouseRightDown()` | `bool` | Shorthand for `MouseDown(Right)` |
| `MouseLeftDownOnce()` | `bool` | Shorthand for `MouseDownOnce(Left)` |
| `MouseRightDownOnce()` | `bool` | Shorthand for `MouseDownOnce(Right)` |

```csharp
if (Input.MouseLeftDownOnce())
    Fire();
```

### Cursor

| Method | Description |
|---|---|
| `SetMouseMode(CursorMode mode)` | Lock (`Raw`) or unlock (`Normal`) the cursor |
| `ToggleMouseMode()` | Toggle between `Raw` and `Normal` cursor modes |

```csharp
Input.SetMouseMode(CursorMode.Raw);
Input.ToggleMouseMode();
```

### Mouse Delta

Accumulated mouse movement this frame, multiplied by sensitivity:

```csharp
Vector2 delta = Input.MouseDelta;
// delta.X = horizontal, delta.Y = vertical
```

### Mouse Wheel

```csharp
float wheel = Input.MouseWheelValue;
if (wheel != 0)
    Camera.FOV -= wheel * 0.05f;
```

### Mouse Sensitivity

```csharp
Input.MouseSensitivity = 0.002f;
```

### Context

| Method | Return | Description |
|---|---|---|
| `GetContext()` | `IInputContext` | Access the underlying Silk.NET input context |

## Complete Example

```csharp
protected override void Update(double dt)
{
    // Toggle mouse lock
    if (Input.KeyDownOnce(Key.Escape))
        Input.ToggleMouseMode();

    // Camera rotation from mouse delta
    var delta = Input.MouseDelta;
    Camera.Yaw += delta.X;
    Camera.Pitch += delta.Y;

    // Edge-detect interaction
    if (Input.KeyDownOnce(Key.E))
        Interact();

    // Continuous action
    if (Input.KeyDown(Key.LeftShift))
        MoveForward(15f * (float)dt);
    else
        MoveForward(5f * (float)dt);

    // Mouse wheel zoom
    float scroll = Input.MouseWheelValue;
    if (scroll != 0)
        Camera.FOV = MathHelper.Clamp(Camera.FOV + scroll, 0.1f, MathF.PI / 2f);
}
```

## Internal Behavior

- `KeyDown()` returns `true` for the frame a key is first pressed AND every frame it is held.
- `KeyDownOnce()` returns `true` only on the frame the key is first pressed (edge detection).
- `MouseDelta` is accumulated each frame and reset on `Update()`.
- `MouseWheelValue` is reset each frame after being read.
- `MouseDownOnce()` tracks pressed buttons with an internal list, same edge-detect pattern as keys.
- The constructor initializes all mice to `CursorMode.Raw` by default.

## See Also

- [Camera](camera.md) — `FreeCamera` uses `Input.MouseDelta` for rotation
- [Gizmos](../rendering/gizmos.md) — F11 toggles render halt via `Input`
