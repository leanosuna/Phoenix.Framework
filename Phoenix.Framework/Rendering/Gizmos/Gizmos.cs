using Phoenix.Framework.Rendering.Primitives;
using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.Collisions;
using Phoenix.Framework.Maths;
using Silk.NET.OpenGL;
using System.Numerics;
using PrimPlane = Phoenix.Framework.Rendering.Primitives.Plane;

namespace Phoenix.Framework.Rendering.Gizmos
{
    public class Gizmos
    {
        private readonly GL GL;
        private readonly PhoenixGame game;
        private readonly GLShader _shader;
        private readonly List<GizmoGeometryInstance> _drawList = new();
        private readonly Primitive _cubeGeometry;
        private readonly Primitive _sphereGeometry;
        private readonly Primitive _cylinderGeometry;
        private readonly Primitive _planeGeometry;
        private readonly VertexArrayObject<float, ushort> _lineVAO;
        private readonly uint _lineIndicesLength;
        private ShaderGizmos ShaderGizmos;
        public bool Enabled { get; set; } = true;
        
        internal Gizmos(PhoenixGame game)
        {
            this.game = game;
            GL = game.GL;
            
            ShaderGizmos = new ShaderGizmos(GL);
            ShaderGizmos.AttachUBO(game.CommonUboHandle, "CommonData");
            
            _cubeGeometry = new Cube(new InfoCube { MeshPrimitiveType = PrimitiveType.Lines });
            _sphereGeometry = new Sphere(new InfoSphere { MeshPrimitiveType = PrimitiveType.Lines });
            _cylinderGeometry = new Cylinder(new InfoCylinder { MeshPrimitiveType = PrimitiveType.Lines });
            _planeGeometry = new PrimPlane(new InfoPlane { MeshPrimitiveType = PrimitiveType.Lines });

            float[] lineVerts = [0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f];
            ushort[] lineIndices = [0, 1];
            _lineIndicesLength = 2;
            var ebo = new BufferObject<ushort>(GL, lineIndices, BufferTargetARB.ElementArrayBuffer);
            var vbo = new BufferObject<float>(GL, lineVerts, BufferTargetARB.ArrayBuffer);
            _lineVAO = new VertexArrayObject<float, ushort>(GL, vbo, ebo);
            _lineVAO.Bind();
            _lineVAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
            GL.BindVertexArray(0);
        }

        internal void Update()
        {
            _drawList.Clear();
        }

        internal void Render()
        {
            if (!Enabled) return;
            if (game.Camera is null) return;
            if (game.Camera.View == Matrix4x4.Identity || game.Camera.Projection == Matrix4x4.Identity) return;

            ShaderGizmos.Use();
            //_shader.SetAsCurrentGLProgram();
            foreach (var instance in _drawList)
            {
                //_shader.SetUniform("uWorld", instance.World);
                //_shader.SetUniform("uColor", instance.Color);
                //_shader.SetUniform("uHit", instance.Hit);

                ShaderGizmos.World.Set(instance.World);
                ShaderGizmos.Color.Set(instance.Color);
                ShaderGizmos.Hit.Set(instance.Hit);

                instance.Draw();
            }
        }

        public void AddLine(Vector3 origin, Vector3 destination, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(destination - origin) * Matrix4x4.CreateTranslation(origin);
            _drawList.Add(new GizmoGeometryInstance { Draw = DrawLine, World = world, Color = color, Hit = hit });
        }

        private unsafe void DrawLine()
        {
            _lineVAO.Bind();
            GL.DrawElements(PrimitiveType.Lines, _lineIndicesLength, DrawElementsType.UnsignedShort, null);
        }

        public void AddCube(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance { Draw = _cubeGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddCube(Vector3 position, Vector3 size, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance { Draw = _cubeGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddSphere(Vector3 position, float radius, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(radius * 2f) * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance { Draw = _sphereGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddSphere(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance { Draw = _sphereGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddCylinder(Vector3 position, float radius, float height, Quaternion rotation, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(radius * 2f, height, radius * 2f)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance { Draw = _cylinderGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddCylinder(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance { Draw = _cylinderGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddPlane(Vector3 position, Vector3 normal, Vector2 size, Vector3 color, bool hit = false)
        {
            Matrix4x4 world = Matrix4x4.CreateScale(size.X, 0, size.Y);
            if (normal == Vector3.Zero)
                normal = Vector3.UnitY;

            normal = Vector3.Normalize(normal);
            var yaw = MathF.Atan2(normal.X, normal.Z);
            var pitch = MathF.Asin(-normal.Y);
            var rot = MathHelper.RotationMxFromYawPitchRoll(yaw, pitch + MathHelper.PiOver2, 0);

            world *= rot;
            world *= Matrix4x4.CreateTranslation(position);

            _drawList.Add(new GizmoGeometryInstance { Draw = _planeGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddPlane(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance { Draw = _planeGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddVolume<T>(T volume, Vector3 color, bool hit = false)
        {
            switch (volume)
            {
                case AxisAlignedBoundingBox box:
                    AddCube(box._world, color, hit); break;
                case OrientedBoundingBox obb:
                    AddCube(obb._world, color, hit); break;
                case BoundingCylinder cyl:
                    AddCylinder(cyl._world, color, hit); break;
                case BoundingSphere sphere:
                    AddSphere(sphere._world, color, hit); break;
                case BoundingFrustum frustum:
                    AddFrustum(frustum.GetCorners(), color, hit); break;
            }
        }

        private readonly int[,] _frustumEdges = new int[,]
        {
            {0,1}, {1,2}, {2,3}, {3,0},
            {4,5}, {5,6}, {6,7}, {7,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        private void AddFrustum(Vector3[] corners, Vector3 color, bool hit = false)
        {
            for (int i = 0; i < _frustumEdges.GetLength(0); i++)
            {
                AddLine(corners[_frustumEdges[i, 0]], corners[_frustumEdges[i, 1]], color, hit);
            }
        }

        public void AddCapsule(Vector3 pointA, Vector3 pointB, float radius, Vector3 color, bool hit = false)
        {
            var axis = pointB - pointA;
            var len = axis.Length();
            if (len < 1e-6f)
            {
                AddSphere(pointA, radius, color, hit);
                return;
            }
            var dir = Vector3.Normalize(axis);

            var perp = MathF.Abs(dir.X) < 0.9f
                ? Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitX))
                : Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY));
            var perp2 = Vector3.Normalize(Vector3.Cross(dir, perp));

            var arcSegs = 16;
            var halfArc = arcSegs / 2;

            void DrawHalfCircle(Vector3 center, float startAngle)
            {
                for (int i = 0; i < halfArc; i++)
                {
                    var a0 = startAngle + i * MathF.Tau / arcSegs;
                    var a1 = startAngle + (i + 1) * MathF.Tau / arcSegs;
                    var p0 = center + perp * MathF.Cos(a0) * radius + perp2 * MathF.Sin(a0) * radius;
                    var p1 = center + perp * MathF.Cos(a1) * radius + perp2 * MathF.Sin(a1) * radius;
                    AddLine(p0, p1, color, hit);
                }
            }

            void DrawDomeArc(Vector3 center, Vector3 outward, Vector3 refPerp)
            {
                for (int i = 0; i < halfArc; i++)
                {
                    var a0 = -MathF.PI / 2 + i * MathF.PI / halfArc;
                    var a1 = -MathF.PI / 2 + (i + 1) * MathF.PI / halfArc;
                    var p0 = center + refPerp * MathF.Sin(a0) * radius + outward * MathF.Cos(a0) * radius;
                    var p1 = center + refPerp * MathF.Sin(a1) * radius + outward * MathF.Cos(a1) * radius;
                    AddLine(p0, p1, color, hit);
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
                var a = i * MathF.Tau / 4f;
                var p = perp * MathF.Cos(a) * radius + perp2 * MathF.Sin(a) * radius;
                AddLine(pointA + p, pointB + p, color, hit);
            }
        }

        public void AddAxisLines(int length)
        {
            AddLine(Vector3.Zero, Vector3.UnitX * length, Vector3.UnitX);
            AddLine(Vector3.Zero, Vector3.UnitY * length, Vector3.UnitY);
            AddLine(Vector3.Zero, Vector3.UnitZ * length, Vector3.UnitZ);
        }
    }
}