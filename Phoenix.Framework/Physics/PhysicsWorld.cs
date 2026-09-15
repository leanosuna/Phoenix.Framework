using Box3D;
using System.Numerics;

namespace Phoenix.Framework.Physics;

/// <summary>
/// Physics simulation manager wrapping Box3D.NET 3D rigid-body dynamics and collision queries.
/// </summary>
public sealed class PhysicsWorld : IDisposable
{
    private readonly Box3D.PhysicsWorld _world;
    private bool _disposed;

    /// <summary>
    /// Gets the underlying native Box3D.NET physics world.
    /// </summary>
    public Box3D.PhysicsWorld World => _world;

    /// <summary>
    /// Gets or sets global gravitational acceleration.
    /// </summary>
    public Vector3 Gravity
    {
        get => _world.Gravity;
        set => _world.Gravity = value;
    }

    /// <summary>
    /// Initializes a 3D rigid-body physics world with customizable gravity and world settings.
    /// </summary>
    public PhysicsWorld(Vector3? gravity = null, WorldSettings? settings = null)
    {
        Vector3 grav = gravity ?? new Vector3(0f, -9.81f, 0f);

        WorldSettings config = settings ?? new WorldSettings
        {
            Gravity = grav
        };

        _world = new Box3D.PhysicsWorld(config);
        _world.Gravity = grav;
    }

    /// <summary>
    /// Advances physics simulation by the specified elapsed delta time using sub-stepping.
    /// </summary>
    public void Step(float deltaTime, int subSteps = 4)
    {
        if (_disposed || deltaTime <= 0f)
            return;

        _world.Step(deltaTime, subSteps);
    }

    /// <summary>
    /// Creates an axis-aligned static box obstacle at the specified position.
    /// </summary>
    public Body CreateStaticBox(Vector3 position, Vector3 size)
    {
        var body = _world.CreateStaticBody(position);
        body.AddBox(new Box(size * 0.5f));
        return body;
    }

    /// <summary>
    /// Creates a dynamic box rigid body subject to forces, impulses, and gravity.
    /// </summary>
    public Body CreateDynamicBox(Vector3 position, Vector3 size)
    {
        var body = _world.CreateDynamicBody(position);
        body.AddBox(new Box(size * 0.5f));
        return body;
    }

    /// <summary>
    /// Creates an immoveable static sphere collider at the specified position.
    /// </summary>
    public Body CreateStaticSphere(Vector3 position, float radius)
    {
        var body = _world.CreateStaticBody(position);
        body.AddSphere(new Sphere(radius));
        return body;
    }

    /// <summary>
    /// Creates a dynamic sphere rigid body at the specified position.
    /// </summary>
    public Body CreateDynamicSphere(Vector3 position, float radius)
    {
        var body = _world.CreateDynamicBody(position);
        body.AddSphere(new Sphere(radius));
        return body;
    }

    /// <summary>
    /// Casts a ray into the physics world and returns the closest hit information.
    /// </summary>
    public RaycastHit Raycast(Vector3 origin, Vector3 direction, float maxDistance = 1000f)
    {
        Vector3 translation = Vector3.Normalize(direction) * maxDistance;
        return _world.RaycastClosest(origin, translation);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _world.Dispose();
    }
}
