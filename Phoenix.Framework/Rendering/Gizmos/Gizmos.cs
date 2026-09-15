using Phoenix.Framework.Collisions;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Gizmos;

/// <summary>
/// Immediate-mode 3D debug visualization system for rendering wireframes, bounds, and spatial data.
/// </summary>
public sealed class Gizmos : IDisposable
{
    private readonly PhoenixGame _game;
    private readonly GizmoRenderer _renderer;
    private readonly List<VertexPositionColor> _lineVertices = [];
    private bool _disposed;

    public bool Enabled { get; set; } = true;
    public bool HasCommands => _lineVertices.Count > 0;

    private static readonly int[,] FrustumEdges =
    {
        {0,1}, {1,2}, {2,3}, {3,0},
        {4,5}, {5,6}, {6,7}, {7,4},
        {0,4}, {1,5}, {2,6}, {3,7}
    };

    public Gizmos(PhoenixGame game)
    {
        _game = game;
        _renderer = new GizmoRenderer(game.Graphics);
    }

    /// <summary>
    /// Clears accumulated gizmo line commands for the next update frame.
    /// </summary>
    public void Update()
    {
        _lineVertices.Clear();
    }

    /// <summary>
    /// Renders all recorded wireframe primitives into the active render pass or a scoped gizmo overlay pass.
    /// </summary>
    public void Render(RenderContext ctx)
    {
        if (!Enabled || _lineVertices.Count == 0)
            return;

        if (ctx.IsPassActive)
        {
            _renderer.Render(ctx, CollectionsMarshal.AsSpan(_lineVertices));
        }
        else
        {
            using (ctx.BeginGizmoPass())
            {
                _renderer.Render(ctx, CollectionsMarshal.AsSpan(_lineVertices));
            }
        }

        _lineVertices.Clear();
    }

    private static Vector4 ResolveColor(Vector4 color, bool hit)
    {
        if (!hit)
            return color;

        if (color.X > 0.9f && color.Y < 0.1f && color.Z < 0.1f)
            return new Vector4(1f, 1f, 0f, color.W);

        return new Vector4(1f, 0f, 0f, color.W);
    }

    private static Vector4 ToV4(Vector3 c) => new(c.X, c.Y, c.Z, 1.0f);

    public void DrawLine(Vector3 start, Vector3 end, Vector4 color, bool hit = false)
    {
        if (!Enabled)
            return;

        Vector4 c = ResolveColor(color, hit);
        _lineVertices.Add(new VertexPositionColor(start, c));
        _lineVertices.Add(new VertexPositionColor(end, c));
    }

    public void DrawLine(Vector3 start, Vector3 end, Vector3 color, bool hit = false)
    {
        DrawLine(start, end, ToV4(color), hit);
    }

    public void DrawRay(Vector3 origin, Vector3 direction, float length, Vector4 color, bool hit = false)
    {
        if (direction == Vector3.Zero)
            return;

        DrawLine(origin, origin + Vector3.Normalize(direction) * length, color, hit);
    }

    public void DrawRay(Ray ray, float length, Vector4 color, bool hit = false)
    {
        DrawRay(ray.Position, ray.Direction, length, color, hit);
    }

    public void DrawBox(Vector3 position, Vector3 size, Vector4 color, bool hit = false)
    {
        Vector3 half = size * 0.5f;
        Vector3 p000 = position + new Vector3(-half.X, -half.Y, -half.Z);
        Vector3 p001 = position + new Vector3(-half.X, -half.Y,  half.Z);
        Vector3 p010 = position + new Vector3(-half.X,  half.Y, -half.Z);
        Vector3 p011 = position + new Vector3(-half.X,  half.Y,  half.Z);
        Vector3 p100 = position + new Vector3( half.X, -half.Y, -half.Z);
        Vector3 p101 = position + new Vector3( half.X, -half.Y,  half.Z);
        Vector3 p110 = position + new Vector3( half.X,  half.Y, -half.Z);
        Vector3 p111 = position + new Vector3( half.X,  half.Y,  half.Z);

        DrawLine(p000, p100, color, hit);
        DrawLine(p100, p110, color, hit);
        DrawLine(p110, p010, color, hit);
        DrawLine(p010, p000, color, hit);

        DrawLine(p001, p101, color, hit);
        DrawLine(p101, p111, color, hit);
        DrawLine(p111, p011, color, hit);
        DrawLine(p011, p001, color, hit);

        DrawLine(p000, p001, color, hit);
        DrawLine(p100, p101, color, hit);
        DrawLine(p110, p111, color, hit);
        DrawLine(p010, p011, color, hit);
    }

    public void DrawBox(AxisAlignedBoundingBox aabb, Vector4 color, bool hit = false)
    {
        DrawBox(aabb.Position, aabb.Size, color, hit);
    }

    public void DrawBox(OrientedBoundingBox obb, Vector4 color, bool hit = false)
    {
        DrawBox(obb._world, color, hit);
    }

    public void DrawBox(Matrix4x4 world, Vector4 color, bool hit = false)
    {
        Span<Vector3> corners = stackalloc Vector3[8]
        {
            Vector3.Transform(new Vector3(-0.5f, -0.5f, -0.5f), world),
            Vector3.Transform(new Vector3( 0.5f, -0.5f, -0.5f), world),
            Vector3.Transform(new Vector3( 0.5f,  0.5f, -0.5f), world),
            Vector3.Transform(new Vector3(-0.5f,  0.5f, -0.5f), world),
            Vector3.Transform(new Vector3(-0.5f, -0.5f,  0.5f), world),
            Vector3.Transform(new Vector3( 0.5f, -0.5f,  0.5f), world),
            Vector3.Transform(new Vector3( 0.5f,  0.5f,  0.5f), world),
            Vector3.Transform(new Vector3(-0.5f,  0.5f,  0.5f), world)
        };

        DrawLine(corners[0], corners[1], color, hit);
        DrawLine(corners[1], corners[2], color, hit);
        DrawLine(corners[2], corners[3], color, hit);
        DrawLine(corners[3], corners[0], color, hit);

        DrawLine(corners[4], corners[5], color, hit);
        DrawLine(corners[5], corners[6], color, hit);
        DrawLine(corners[6], corners[7], color, hit);
        DrawLine(corners[7], corners[4], color, hit);

        DrawLine(corners[0], corners[4], color, hit);
        DrawLine(corners[1], corners[5], color, hit);
        DrawLine(corners[2], corners[6], color, hit);
        DrawLine(corners[3], corners[7], color, hit);
    }

    public void DrawSphere(Vector3 center, float radius, Vector4 color, bool hit = false, int segments = 24)
    {
        if (!Enabled || radius <= 0f)
            return;

        float step = MathF.Tau / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;

            Vector3 xy0 = center + new Vector3(MathF.Cos(a0) * radius, MathF.Sin(a0) * radius, 0f);
            Vector3 xy1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0f);
            DrawLine(xy0, xy1, color, hit);

            Vector3 xz0 = center + new Vector3(MathF.Cos(a0) * radius, 0f, MathF.Sin(a0) * radius);
            Vector3 xz1 = center + new Vector3(MathF.Cos(a1) * radius, 0f, MathF.Sin(a1) * radius);
            DrawLine(xz0, xz1, color, hit);

            Vector3 yz0 = center + new Vector3(0f, MathF.Cos(a0) * radius, MathF.Sin(a0) * radius);
            Vector3 yz1 = center + new Vector3(0f, MathF.Cos(a1) * radius, MathF.Sin(a1) * radius);
            DrawLine(yz0, yz1, color, hit);
        }
    }

    public void DrawSphere(BoundingSphere sphere, Vector4 color, bool hit = false)
    {
        DrawSphere(sphere.Position, sphere.Radius, color, hit);
    }

    public void DrawSphere(Matrix4x4 world, Vector4 color, bool hit = false)
    {
        Vector3 position = world.Translation;
        float radius = MathF.Max(world.M11, MathF.Max(world.M22, world.M33)) * 0.5f;
        DrawSphere(position, radius, color, hit);
    }

    public void DrawFrustum(BoundingFrustum frustum, Vector4 color, bool hit = false)
    {
        DrawFrustum(frustum.GetCorners(), color, hit);
    }

    public void DrawFrustum(Vector3[] corners, Vector4 color, bool hit = false)
    {
        if (corners.Length < 8)
            return;

        for (int i = 0; i < FrustumEdges.GetLength(0); i++)
        {
            DrawLine(corners[FrustumEdges[i, 0]], corners[FrustumEdges[i, 1]], color, hit);
        }
    }

    public void DrawCylinder(Vector3 position, float radius, float height, Quaternion rotation, Vector4 color, bool hit = false, int segments = 24)
    {
        if (!Enabled)
            return;

        float halfH = height * 0.5f;
        float step = MathF.Tau / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;

            Vector3 b0 = Vector3.Transform(new Vector3(MathF.Cos(a0) * radius, -halfH, MathF.Sin(a0) * radius), rotation) + position;
            Vector3 b1 = Vector3.Transform(new Vector3(MathF.Cos(a1) * radius, -halfH, MathF.Sin(a1) * radius), rotation) + position;
            DrawLine(b0, b1, color, hit);

            Vector3 t0 = Vector3.Transform(new Vector3(MathF.Cos(a0) * radius, halfH, MathF.Sin(a0) * radius), rotation) + position;
            Vector3 t1 = Vector3.Transform(new Vector3(MathF.Cos(a1) * radius, halfH, MathF.Sin(a1) * radius), rotation) + position;
            DrawLine(t0, t1, color, hit);

            if (i % (segments / 4) == 0)
            {
                DrawLine(b0, t0, color, hit);
            }
        }
    }

    public void DrawCylinder(BoundingCylinder cylinder, Vector4 color, bool hit = false)
    {
        DrawCylinder(cylinder.Position, cylinder.Radius, cylinder.Height, Quaternion.CreateFromRotationMatrix(cylinder.Rotation), color, hit);
    }

    public void DrawCylinder(Matrix4x4 world, Vector4 color, bool hit = false)
    {
        Matrix4x4.Decompose(world, out var scale, out var rot, out var trans);
        DrawCylinder(trans, scale.X * 0.5f, scale.Y, rot, color, hit);
    }

    public void DrawCapsule(Vector3 pointA, Vector3 pointB, float radius, Vector4 color, bool hit = false)
    {
        var axis = pointB - pointA;
        float len = axis.Length();
        if (len < 1e-6f)
        {
            DrawSphere(pointA, radius, color, hit);
            return;
        }

        var dir = Vector3.Normalize(axis);
        var perp = MathF.Abs(dir.X) < 0.9f
            ? Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitX))
            : Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY));
        var perp2 = Vector3.Normalize(Vector3.Cross(dir, perp));

        int arcSegs = 16;
        int halfArc = arcSegs / 2;

        void DrawHalfCircle(Vector3 center, float startAngle)
        {
            for (int i = 0; i < halfArc; i++)
            {
                float a0 = startAngle + i * MathF.Tau / arcSegs;
                float a1 = startAngle + (i + 1) * MathF.Tau / arcSegs;
                Vector3 p0 = center + perp * MathF.Cos(a0) * radius + perp2 * MathF.Sin(a0) * radius;
                Vector3 p1 = center + perp * MathF.Cos(a1) * radius + perp2 * MathF.Sin(a1) * radius;
                DrawLine(p0, p1, color, hit);
            }
        }

        void DrawDomeArc(Vector3 center, Vector3 outward, Vector3 refPerp)
        {
            for (int i = 0; i < halfArc; i++)
            {
                float a0 = -MathF.PI / 2 + i * MathF.PI / halfArc;
                float a1 = -MathF.PI / 2 + (i + 1) * MathF.PI / halfArc;
                Vector3 p0 = center + refPerp * MathF.Sin(a0) * radius + outward * MathF.Cos(a0) * radius;
                Vector3 p1 = center + refPerp * MathF.Sin(a1) * radius + outward * MathF.Cos(a1) * radius;
                DrawLine(p0, p1, color, hit);
            }
        }

        DrawHalfCircle(pointA, 0f);
        DrawHalfCircle(pointA, MathF.PI);
        DrawHalfCircle(pointB, 0f);
        DrawHalfCircle(pointB, MathF.PI);

        DrawDomeArc(pointA, -dir, perp);
        DrawDomeArc(pointA, -dir, perp2);
        DrawDomeArc(pointB, dir, perp);
        DrawDomeArc(pointB, dir, perp2);

        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f;
            Vector3 p = perp * MathF.Cos(a) * radius + perp2 * MathF.Sin(a) * radius;
            DrawLine(pointA + p, pointB + p, color, hit);
        }
    }

    public void DrawCapsule(Capsule capsule, Vector4 color, bool hit = false)
    {
        DrawCapsule(capsule.PointA, capsule.PointB, capsule.Radius, color, hit);
    }

    public void DrawPlane(Vector3 position, Vector3 normal, Vector2 size, Vector4 color, bool hit = false)
    {
        if (normal == Vector3.Zero)
            normal = Vector3.UnitY;

        normal = Vector3.Normalize(normal);

        Vector3 worldUp = MathF.Abs(normal.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
        Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, normal));
        Vector3 forward = Vector3.Normalize(Vector3.Cross(normal, right));

        Vector3 halfRight = right * (size.X * 0.5f);
        Vector3 halfForward = forward * (size.Y * 0.5f);

        Vector3 p0 = position - halfRight - halfForward;
        Vector3 p1 = position + halfRight - halfForward;
        Vector3 p2 = position + halfRight + halfForward;
        Vector3 p3 = position - halfRight + halfForward;

        DrawLine(p0, p1, color, hit);
        DrawLine(p1, p2, color, hit);
        DrawLine(p2, p3, color, hit);
        DrawLine(p3, p0, color, hit);
        DrawLine(position, position + normal * 0.5f, color, hit);
    }

    public void DrawPlane(Matrix4x4 world, Vector4 color, bool hit = false)
    {
        Vector3 pos = world.Translation;
        Vector3 normal = Vector3.Normalize(new Vector3(world.M21, world.M22, world.M23));
        float sx = new Vector3(world.M11, world.M12, world.M13).Length();
        float sz = new Vector3(world.M31, world.M32, world.M33).Length();
        DrawPlane(pos, normal, new Vector2(sx, sz), color, hit);
    }

    public void DrawAxisLines(Vector3 position, float length = 1.0f)
    {
        DrawLine(position, position + Vector3.UnitX * length, new Vector4(1, 0, 0, 1));
        DrawLine(position, position + Vector3.UnitY * length, new Vector4(0, 1, 0, 1));
        DrawLine(position, position + Vector3.UnitZ * length, new Vector4(0, 0, 1, 1));
    }

    public void AddVolume<T>(T volume, Vector3 color, bool hit = false)
    {
        Vector4 c = ToV4(color);
        switch (volume)
        {
            case AxisAlignedBoundingBox box:
                DrawBox(box, c, hit);
                break;
            case OrientedBoundingBox obb:
                DrawBox(obb, c, hit);
                break;
            case BoundingCylinder cyl:
                DrawCylinder(cyl, c, hit);
                break;
            case BoundingSphere sphere:
                DrawSphere(sphere, c, hit);
                break;
            case BoundingFrustum frustum:
                DrawFrustum(frustum, c, hit);
                break;
            case Capsule capsule:
                DrawCapsule(capsule, c, hit);
                break;
        }
    }

    public void AddCube(Matrix4x4 world, Vector3 color, bool hit = false) => DrawBox(world, ToV4(color), hit);
    public void AddCube(Vector3 position, Vector3 size, Vector3 color, bool hit = false) => DrawBox(position, size, ToV4(color), hit);
    public void AddSphere(Vector3 position, float radius, Vector3 color, bool hit = false) => DrawSphere(position, radius, ToV4(color), hit);
    public void AddSphere(Matrix4x4 world, Vector3 color, bool hit = false) => DrawSphere(world, ToV4(color), hit);
    public void AddCylinder(Vector3 position, float radius, float height, Quaternion rotation, Vector3 color, bool hit = false) => DrawCylinder(position, radius, height, rotation, ToV4(color), hit);
    public void AddCylinder(Matrix4x4 world, Vector3 color, bool hit = false) => DrawCylinder(world, ToV4(color), hit);
    public void AddCapsule(Vector3 pointA, Vector3 pointB, float radius, Vector3 color, bool hit = false) => DrawCapsule(pointA, pointB, radius, ToV4(color), hit);
    public void AddPlane(Vector3 position, Vector3 normal, Vector2 size, Vector3 color, bool hit = false) => DrawPlane(position, normal, size, ToV4(color), hit);
    public void AddPlane(Matrix4x4 world, Vector3 color, bool hit = false) => DrawPlane(world, ToV4(color), hit);
    public void AddFrustum(Vector3[] corners, Vector3 color, bool hit = false) => DrawFrustum(corners, ToV4(color), hit);
    public void AddLine(Vector3 origin, Vector3 destination, Vector3 color, bool hit = false) => DrawLine(origin, destination, ToV4(color), hit);
    public void AddRay(Vector3 origin, Vector3 direction, float length, Vector3 color, bool hit = false) => DrawRay(origin, direction, length, ToV4(color), hit);
    public void AddAxisLines(int length) => DrawAxisLines(Vector3.Zero, length);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _renderer.Dispose();
    }
}
