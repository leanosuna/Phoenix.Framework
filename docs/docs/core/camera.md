# Camera

Namespace: `Phoenix.Framework.Cameras`

Phoenix provides a hierarchy of camera classes for 3D viewport control. Cameras feed `View` and `Projection` matrices into `FrameUniforms` (Set 0) every frame.

---

## Camera hierarchy

```
Camera (abstract)
 └── BaseCamera (abstract)
      └── MouseCamera (abstract)
           └── FreeCamera (concrete)
```

| Class | Purpose |
|---|---|
| `Camera` | Base class defining `View`, `Projection`, and `Position` |
| `BaseCamera` | Shared state: position, yaw, pitch, FOV, near/far planes, aspect ratio |
| `MouseCamera` | Adds mouse delta input for yaw and pitch look controls |
| `FreeCamera` | Concrete camera supporting WASD keyboard movement and mouse aim |

---

## FreeCamera

`FreeCamera` is the standard first-person camera. It supports keyboard translation and mouse look.

### Creating a FreeCamera

```csharp
Camera = new FreeCamera(
    game: this,                           // PhoenixGame reference
    position: new Vector3(0, 5, -10),     // Starting position
    yaw: 0,                               // Yaw (radians)
    pitch: -MathF.PI / 4,                 // Pitch (radians)
    fov: MathF.PI / 4,                    // FOV (radians)
    nearPlane: 0.1f,                      // Near clipping plane
    farPlane: 1000f,                      // Far clipping plane
    aspectRatio: WindowWidth / (float)WindowHeight
);
```

### Movement keys

```csharp
camera.SetMoveKeys(
    Key.W,          // Forward
    Key.S,          // Backward
    Key.A,          // Left
    Key.D,          // Right
    Key.E,          // Up
    Key.Q,          // Down
    Key.LeftShift,  // Sprint key
    moveSpeedMultiplier: 2f
);

camera.MoveSpeed = 15f;  // Base speed in units per second
```

### Mouse look

Mouse look is active by default (`MouseAim = true`). Mouse deltas are automatically converted to yaw and pitch rotations each frame.

```csharp
Input.MouseSensitivity = 0.002f;  // Default is 0.001f
```

### Updating the camera

Call `Update()` each frame in your game `Update()` method:

```csharp
protected override void Update(double deltaTime)
{
    ((FreeCamera)Camera!).Update(deltaTime);
}
```

This method:
1. Translates position based on pressed movement keys.
2. Rotates yaw and pitch using mouse movement.
3. Recalculates the view matrix using `Matrix4x4.CreateLookAt`.
4. Recalculates the projection matrix using `Matrix4x4.CreatePerspectiveFieldOfView`.

---

## Camera properties

| Property | Type | Description |
|---|---|---|
| `Position` | `Vector3` | Camera position in world space |
| `Front` | `Vector3` | Forward direction vector (normalized) |
| `Up` | `Vector3` | Up direction vector (normalized) |
| `Right` | `Vector3` | Right direction vector (normalized) |
| `Yaw` | `float` | Horizontal rotation angle in radians |
| `Pitch` | `float` | Vertical rotation angle in radians |
| `FOV` | `float` | Field of view in radians |
| `AspectRatio` | `float` | Framebuffer aspect ratio (width / height) |
| `NearPlane` | `float` | Near clipping plane distance |
| `FarPlane` | `float` | Far clipping plane distance |
| `View` | `Matrix4x4` | View transformation matrix |
| `Projection` | `Matrix4x4` | Perspective projection matrix |
| `MoveSpeed` | `float` | Base movement units per second (default 10) |
| `MouseAim` | `bool` | Whether mouse deltas rotate the camera |

---

## Integration with FrameUniforms

The camera view and projection matrices are uploaded to Set 0 (`FrameUniforms`) automatically before each frame render. In your GLSL shaders:

```glsl
layout(set = 0, binding = 0) uniform FrameUniforms {
    mat4 uView;
    mat4 uProjection;
    mat4 uViewProjection;
    mat4 uInverseView;
    mat4 uInverseProjection;
    vec4 uCameraPosition;
    vec2 uResolution;
    float uTime;
    float uDeltaTime;
};
```

---

## Custom cameras

To build a custom camera (such as an orbit or third-person camera), inherit from `BaseCamera`:

```csharp
public class OrbitCamera : BaseCamera
{
    public float Distance = 10f;

    public OrbitCamera(Vector3 position, float yaw, float pitch, float fov,
        float nearPlane, float farPlane, float aspectRatio)
        : base(position, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)
    {
    }

    public override void Update(double deltaTime)
    {
        CalculateVectors();
        CalculateView();
        CalculateProjection();
    }
}
```
