# Input

The `InputManager` handles keyboard and mouse input via Silk.NET.

## Access

`InputManager` is a read-only property on `PhoenixGame`:

```csharp
var input = InputManager;  // or: this.InputManager
```

## Keyboard

### Key State Queries

```csharp
// Is the key currently held down this frame?
if (InputManager.KeyDown(Key.W))
{
    // Move forward
}

// Fires only the first frame a key is down, 
// Resets when releasing it.
if (InputManager.KeyDownOnce(Key.E))
{
    // Toggle something...
    InputManager.ToggleMouseMode();
}
```

## Mouse

### Mouse Delta

Accumulated mouse movement this frame, multiplied by sensitivity:

```csharp
Vector2 delta = InputManager.MouseDelta;
// delta.X = horizontal movement
// delta.Y = vertical movement
```

### Mouse Wheel

```csharp
protected override void Update(double dt)
{
    float wheel = InputManager.MouseWheelValue;
    if (wheel != 0)
    {
        // Zoom in/out
        Camera.FOV -= wheel * 0.05f;
    }
}
```

### Mouse Mode

Lock the cursor to the window (raw input) or allow free movement:

```csharp
// Lock cursor (raw mouse input)
InputManager.SetMouseMode(CursorMode.Raw);

// Unlock cursor (normal mode)
InputManager.SetMouseMode(CursorMode.Normal);

// Toggle between the two
InputManager.ToggleMouseMode();
```

### Mouse Sensitivity

```csharp
InputManager.MouseSensitivity = 0.002f;  // Default: 0.001f
```

## Complete Example

```csharp
protected override void Update(double dt)
{
    // Toggle mouse lock
    if (InputManager.KeyDownOnce(Key.Escape))
        InputManager.ToggleMouseMode();

    // Camera rotation from mouse delta
    if (InputManager.MouseAim)
    {
        Camera.Yaw += InputManager.MouseDelta.X * InputManager.MouseSensitivity;
        Camera.Pitch += InputManager.MouseDelta.Y * InputManager.MouseSensitivity;
    }

    // Edge-detect interaction
    if (InputManager.KeyDownOnce(Key.E))
    {
        InteractWithClosestObject();
    }

    // Continuous action
    if (InputManager.KeyDown(Key.LeftShift))
    {
        MoveForward(15f * (float)dt);  // Sprint
    }
    else
    {
        MoveForward(5f * (float)dt);   // Walk
    }

    // Mouse wheel zoom
    if (InputManager.MouseWheelValue != 0)
    {
        Camera.FOV = MathHelper.Clamp(Camera.FOV + InputManager.MouseWheelValue, 0.1f, MathF.PI / 2f);
    }
}
```

## Internal Behavior

- `KeyDown()` returns `true` for the frame a key is first pressed AND every frame it is held.
- `KeyDownOnce()` returns `true` only on the frame the key is first pressed (edge detection).
- `MouseDelta` is accumulated from the `OnMouseMove` event and reset each `Update()`.
- `MouseWheelValue` is reset each frame after being read.
- `keysDown` list tracks which keys have been pressed / released each frame.

## See Also

- [Camera](camera.md) — `FreeCamera` uses `InputManager.MouseDelta` for rotation
- [Gizmos](../rendering/gizmos.md) — F11 toggles render halt via `InputManager`
