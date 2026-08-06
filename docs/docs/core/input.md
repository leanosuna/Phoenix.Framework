# Input

Namespace: `Phoenix.Framework.Inputs`

The `Input` class handles keyboard and mouse input via Silk.NET. Supports any number of connected keyboards and mice. Exposed as `PhoenixGame.Input`.

## Properties

| Name | Type | Description |
|---|---|---|
| `MouseSensitivity` | `float` | Mouse delta multiplier (default `0.001f`) |
| `MouseDelta` | `Vector2` | Accumulated mouse movement this frame, multiplied by sensitivity |
| `MouseWheelPrecise` | `float` | Accumulated scroll (raw float value from os) |
| `MouseWheelValue` | `int` | Accumulated scroll in discrete steps |

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

Scroll is exposed in two ways: a precise accumulated value and a whole-notch counter.

```csharp
// Precise scroll delta (fractional, e.g. 0.25, 1.0, -1.5). Never resets.
float precise = Input.MouseWheelPrecise;

// Whole-notch counter: goes up/down by 1 each time MouseWheelPrecise
// crosses an integer boundary (e.g. 0.9 -> 1.0 fires one notch).
int notches = Input.MouseWheelValue;
```

```csharp
// Move using precise values
Camera.Position.Z = Input.MouseWheelPrecise;

// Move using notches
Camera.Position.Z = Input.MouseWheelValue;
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

    Camera.Position.Z = Input.MouseWheelValue;
        
}
```

## Internal Behavior

- `KeyDown()` returns `true` for the frame a key is first pressed AND every frame it is held.
- `KeyDownOnce()` returns `true` only on the frame the key is first pressed (edge detection).
- `MouseDelta` is accumulated each frame and reset on `Update()`.
- `MouseWheelPrecise` accumulates the raw scroll delta and is never reset — track a baseline yourself if you need a per-frame delta.
- `MouseWheelValue` counts whole notches: it increments when `MouseWheelPrecise` passes an integer going up, and decrements when it passes an integer going down.
- `MouseDownOnce()` tracks pressed buttons with an internal list, same edge-detect pattern as keys.
- The constructor initializes all mice to `CursorMode.Raw` by default.

## See Also

- [Camera](camera.md) — `FreeCamera` uses `Input.MouseDelta` for rotation
- [Gizmos](../rendering/gizmos.md) — F11 toggles render halt via `Input`
