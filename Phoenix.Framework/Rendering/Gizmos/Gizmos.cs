using Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives;
using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.Collisions;
using Phoenix.Framework.Maths;
using Silk.NET.OpenGL;
using System.Numerics;
using PrimPlane = Phoenix.Framework.Rendering.Gizmos.Geometries.Primitives.Plane;

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

        public void Update()
        {
            _drawList.Clear();
        }

        public void Render()
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
            var world = Matrix4x4.CreateScale(radius) * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance { Draw = _sphereGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddSphere(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance { Draw = _sphereGeometry.Draw, World = world, Color = color, Hit = hit });
        }

        public void AddCylinder(Vector3 position, float radius, float height, Quaternion rotation, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(radius, height, radius)
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

        public void AddAxisLines(int length)
        {
            AddLine(Vector3.Zero, Vector3.UnitX * length, Vector3.UnitX);
            AddLine(Vector3.Zero, Vector3.UnitY * length, Vector3.UnitY);
            AddLine(Vector3.Zero, Vector3.UnitZ * length, Vector3.UnitZ);
        }
    }
}