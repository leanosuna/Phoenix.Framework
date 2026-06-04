# Camera

Phoenix provides a hierarchy of camera classes for viewport control. Cameras feed `View` and `Projection` matrices into the `CommonUBO` every frame.

## Camera Hierarchy

```
Camera (abstract)
 └── BaseCamera (abstract)
      └── MouseCamera (abstract)
           └── FreeCamera (concrete)
```

| Class | Purpose |
|-------|---------|
| `Camera` | Base class defining `View` and `Projection` |
| `BaseCamera` | Shared state: position, yaw, pitch, FOV, aspect ratio |
| `MouseCamera` | Adds mouse delta input for yaw/pitch control |
| `FreeCamera` | WASD-style movement + mouse look |

## FreeCamera

The main camera class. Supports keyboard movement and mouse look.

### Creating a FreeCamera

```csharp
Camera = new FreeCamera(
    this,                           // PhoenixGame reference
    new Vector3(0, 5, -10),         // Position
    0,                              // Yaw (radians)
    -MathF.PI / 4,                  // Pitch (radians)
    MathF.PI / 4,                   // Max pitch (prevents flipping)
    0.1f,                           // Near plane
    1000f,                          // Far plane
    WindowWidth / (float)WindowHeight  // Aspect ratio
);
```

### Movement Keys

```csharp
// WASD + QE (up/down) + Shift (sprint)
camera.SetMoveKeys(
    Key.W,    // Forward
    Key.S,    // Backward
    Key.A,    // Left
    Key.D,    // Right
    Key.E,    // Up
    Key.Q,    // Down
    Key.LeftShift,  // Speed multiplier key
    moveSpeedMultiplier: 2f   // Sprint multiplier
);

camera.MoveSpeed = 15f;  // Base movement speed
```

### Mouse Look

Mouse look is enabled by default (`MouseAim = true`). Mouse delta is automatically applied to yaw and pitch each frame.

```csharp
camera.MouseSensitivity = 0.002f;  // Override default (0.001f)
```

### Manual Yaw/Pitch Keys

For keyboards without mouse, you can set arrow keys for yaw/pitch:

```csharp
camera.SetPitchYawKeys(
    Key.Up,    // Increase pitch
    Key.Down,  // Decrease pitch
    Key.Left,  // Decrease yaw
    Key.Right, // Increase yaw
    new Vector2(2f, 2f)  // Speed multipliers [pitch, yaw]
);
```

### Updating the Camera

Call `Update()` every frame in your `Update()` method:

```csharp
protected override void Update(double dt)
{
    Camera.Update((float)dt);
}
```

This processes:
- Mouse delta → yaw/pitch rotation
- Keyboard input → position translation
- Recalculates `View` matrix (LookAt)
- Recalculates `Projection` matrix (perspective)

## Camera Properties

| Property | Type | Description |
|----------|------|-------------|
| `Position` | `Vector3` | Camera position |
| `Front` | `Vector3` | Forward direction (normalized) |
| `Up` | `Vector3` | Up vector (normalized) |
| `Right` | `Vector3` | Right vector (normalized) |
| `Yaw` | `float` | Horizontal rotation (radians) |
| `Pitch` | `float` | Vertical rotation (radians) |
| `FOV` | `float` | Field of view (radians) |
| `AspectRatio` | `float` | Width / Height |
| `NearPlane` | `float` | Near clipping plane |
| `FarPlane` | `float` | Far clipping plane |
| `View` | `Matrix4x4` | View matrix (set each frame) |
| `Projection` | `Matrix4x4` | Projection matrix (set each frame) |
| `MoveSpeed` | `float` | Movement speed (default 10) |
| `MouseAim` | `bool` | Whether to process mouse delta (default true) |
| `MouseSensitivity` | `float` | Mouse sensitivity (default 0.001) |

## Camera Usage

The camera's `View` and `Projection` matrices are automatically fed into the `CommonUBO` every frame. Shaders that need camera data bind this UBO:

```csharp
// In your shader:
layout(std140) uniform CommonData
{
    mat4 sView;
    mat4 sProjection;
    float sTime;
    float sDeltaTime;
};

// gl_Position = sProjection * sView * World * vPosition;
```

## Custom Camera

For custom camera behavior, inherit from `BaseCamera`:

```csharp
public class OrbitCamera : BaseCamera
{
    public float Distance;

    public OrbitCamera(PhoenixGame game, Vector3 target, float distance,
        float yaw, float pitch, float fov, float nearPlane, float farPlane,
        float aspectRatio)
        : base(game, target, distance, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)
    {
        Distance = distance;
    }

    public override void Update(float deltaTime)
    {
        // Custom orbit logic
        CalculateVectors();
        CalculateView();
        CalculateProjection();
    }
}
```

## See Also

- [Gizmos](../rendering/gizmos.md) — gizmos use `Camera.View` and `Camera.Projection`
- [BoundingFrustum](../collisions/frustum-box.md) — frustum is built from `Camera.View * Camera.Projection`
