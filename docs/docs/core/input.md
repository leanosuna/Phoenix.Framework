# Input

Namespace: `Phoenix.Framework.Inputs`

The `Input` class handles keyboard and mouse input through Silk.NET. It supports multiple connected keyboards and mice, edge-detected press queries, and cursor lock modes. Exposed as `PhoenixGame.Input`.

---

## Properties

| Property | Type | Description |
|---|---|---|
| `MouseSensitivity` | `float` | Mouse delta multiplier (default `0.001f`) |
| `MouseDelta` | `Vector2` | Accumulated mouse movement this frame multiplied by sensitivity |
| `MouseWheelPrecise` | `float` | Continuous scroll accumulation from the operating system |
| `MouseWheelValue` | `int` | Accumulated scroll counter in discrete integer notches |

---

## Keyboard queries

| Method | Return | Description |
|---|---|---|
| `KeyDown(Key key)` | `bool` | True while the key is held down |
| `KeyDownOnce(Key key)` | `bool` | True only on the frame the key was first pressed |
| `KeyUp(Key key)` | `bool` | True while the key is released |
| `KeyUpOnce(Key key)` | `bool` | True only on the frame the key was released |

```csharp
if (Input.KeyDown(Key.W))
    MoveForward((float)deltaTime);

if (Input.KeyDownOnce(Key.E))
    Interact();
```

---

## Mouse queries

| Method | Return | Description |
|---|---|---|
| `MouseDown(MouseButton button)` | `bool` | True while the button is held down |
| `MouseDownOnce(MouseButton button)` | `bool` | True only on the frame the button was first pressed |
| `MouseLeftDown()` | `bool` | Shorthand for `MouseDown(MouseButton.Left)` |
| `MouseRightDown()` | `bool` | Shorthand for `MouseDown(MouseButton.Right)` |
| `MouseLeftDownOnce()` | `bool` | Shorthand for `MouseDownOnce(MouseButton.Left)` |
| `MouseRightDownOnce()` | `bool` | Shorthand for `MouseDownOnce(MouseButton.Right)` |

```csharp
if (Input.MouseLeftDownOnce())
    FireWeapon();
```

---

## Cursor modes and mouse lock

| Method | Description |
|---|---|
| `SetMouseMode(CursorMode mode)` | Set cursor to `Raw` (hidden and locked to window center) or `Normal` |
| `ToggleMouseMode()` | Toggle between `Raw` and `Normal` cursor modes |

```csharp
Input.SetMouseMode(CursorMode.Raw);
```

---

## Mouse wheel and delta

- **Mouse delta**: `Input.MouseDelta` provides accumulated X and Y movement during the frame, multiplied by `MouseSensitivity`. Reset on each `Input.Update()`.
- **Mouse scroll**:
  - `Input.MouseWheelPrecise`: Fractional cumulative float value.
  - `Input.MouseWheelValue`: Discrete integer notch counter.

```csharp
// Zoom camera with scroll notches
Camera.FOV -= Input.MouseWheelValue * 0.05f;
```

---

## Frame update cycle

- `KeyDown()` returns true for the initial press and all subsequent frames while held.
- `KeyDownOnce()` edge-detects the press, returning true for exactly one frame.
- `MouseDelta` accumulates across OS mouse events and flushes at each `Input.Update()`.
- When render halt (F11) triggers, `Input` restores cursor mode so UI inspectors are immediately usable.
