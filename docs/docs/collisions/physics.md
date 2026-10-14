# Physics world

Namespace: `Phoenix.Framework.Physics`

`PhysicsWorld` provides 3D rigid-body simulation wrapping native Box3D.NET. It manages dynamic bodies, forces, gravity, constraints, and physics raycasts.

---

## Creating the physics world

Instantiate `PhysicsWorld` during `Initialize()`:

```csharp
// Default Earth gravity: (0, -9.81, 0)
_physics = new PhysicsWorld(new Vector3(0, -9.81f, 0));
```

You can customize simulation settings by passing a `WorldSettings` record:

```csharp
var settings = WorldSettings.Default with
{
    Gravity = new Vector3(0, -15f, 0)
};
_physics = new PhysicsWorld(settings: settings);
```

---

## Creating bodies

### Static obstacles
Static bodies are fixed in space and unaffected by gravity or forces. Dynamic bodies collide and slide against them:

```csharp
// Static floor box
Body ground = _physics.CreateStaticBox(
    position: new Vector3(0, -0.5f, 0),
    size: new Vector3(100, 1, 100)
);

// Static sphere obstacle
Body boulder = _physics.CreateStaticSphere(
    position: new Vector3(5, 1, 0),
    radius: 2.0f
);
```

### Dynamic rigid bodies
Dynamic bodies move according to forces, impulses, gravity, and contacts:

```csharp
// Dynamic box crate
Body crate = _physics.CreateDynamicBox(
    position: new Vector3(0, 10, 0),
    size: new Vector3(1, 1, 1)
);

// Dynamic bouncing sphere
Body ball = _physics.CreateDynamicSphere(
    position: new Vector3(2, 12, 0),
    radius: 0.5f
);
```

---

## Advancing the simulation

In your game `Update()` method, step the simulation using sub-stepping to ensure stable collisions:

```csharp
protected override void Update(double deltaTime)
{
    // Sub-stepping breaks deltaTime into 4 smaller solver iterations
    _physics.Step((float)deltaTime, subSteps: 4);

    // Synchronize visual transform from physics body
    _crateWorldMatrix = Matrix4x4.CreateFromQuaternion(crate.Rotation)
        * Matrix4x4.CreateTranslation(crate.Position);
}
```

---

## Physics raycasting

Query the world along a ray direction:

```csharp
RaycastHit hit = _physics.Raycast(
    origin: Camera.Position,
    direction: Camera.Front,
    maxDistance: 100f
);

if (hit.HasHit)
{
    Vector3 hitPoint = hit.Point;
    Vector3 hitNormal = hit.Normal;
    Body hitBody = hit.Body;
}
```

---

## Disposal

Always dispose `PhysicsWorld` in `OnClose()` to release native physics memory:

```csharp
protected override void OnClose()
{
    _physics.Dispose();
}
```
