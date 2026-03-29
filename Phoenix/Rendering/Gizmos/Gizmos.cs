using Silk.NET.OpenGL;
using Phoenix.Collisions;
using Phoenix.Rendering.Gizmos.Geometries;
using System.Numerics;
using Phoenix.Rendering.Shaders;

namespace Phoenix.Rendering.Gizmos
{
    /// <summary>
    /// Gizmos are line based geometries useful to view bounding volumes, and visually verify
    /// collisions with different colors
    /// </summary>
    public class Gizmos
    {
        GL GL;
        PhoenixGame game;
        public bool Enabled { get; set; } = true;
        GLShader _shader;
        List<GizmoGeometryInstance> _drawList = new();

        private GGCube _cubeGeometry;
        private GGLineSegment _lineGeometry;
        private GGCylinder _cylinderGeometry;
        private GGSphere _sphereGeometry;
        private GGPlane _planeGeometry;
        internal Gizmos(PhoenixGame game)
        {
            this.game = game;
            GL = game.GL;
            _shader = new GLShader(GL,
                EmbeddedHelper.ExtractPath("gizmos.vert", "Files.Shaders.gizmos"),
                EmbeddedHelper.ExtractPath("gizmos.frag", "Files.Shaders.gizmos"));
            _shader.AttachUBO(game.CommonUboHandle, "CommonData");

            _cubeGeometry = new GGCube(GL);
            _lineGeometry = new GGLineSegment(GL);
            _sphereGeometry = new GGSphere(GL);
            _cylinderGeometry = new GGCylinder(GL);
            _planeGeometry = new GGPlane(GL);
        }
        /// <summary>
        /// Clears the draw list and updates the gizmos camera matrices
        /// </summary>
        /// <param name="view">The current camera view to apply </param>
        /// <param name="projection">The current camera projection to apply </param>
        public void Update()
        {
            _drawList.Clear();
            //_view = view;
            //_proj = projection;
        }
        /// <summary>
        /// Draws the gizmos added to the drawlist this frame with the internal shader
        /// </summary>
        public void Render()
        {
            if (!Enabled)
                return;

            if (game.Camera.View == Matrix4x4.Identity || game.Camera.Projection == Matrix4x4.Identity)
                return;

            _shader.SetAsCurrentGLProgram();
            foreach (var gizmoInstance in _drawList)
            {
                _shader.SetUniform("uWorld", gizmoInstance.World);
                _shader.SetUniform("uColor", gizmoInstance.Color);
                _shader.SetUniform("uHit", gizmoInstance.Hit);

                gizmoInstance.Geometry.Draw();
            }
        }
        /// <summary>
        /// Generates evenly spaced positions around a unit circle.
        /// </summary>
        internal static Vector2[] GenerateCirclePositions(int subdivisions)
        {
            var positions = new Vector2[subdivisions];

            var offset = 0f;

            // Odd? Then start at 90 degrees
            if (subdivisions % 2 == 1)
                offset = MathHelper.PiOver2;

            var increment = MathHelper.TwoPi / subdivisions;
            for (ushort index = 0; index < subdivisions; index++)
            {
                positions[index] = new Vector2(MathF.Cos(offset), MathF.Sin(offset));
                offset += increment;
            }

            return positions;
        }
        /// <summary>
        /// Adds a line in 3D space to the drawlist
        /// </summary>
        /// <param name="origin">Origin 3D coordinate for the line</param>
        /// <param name="destination">Destination 3D coordinate for the line</param>
        /// <param name="color">The color of the line</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddLine(Vector3 origin, Vector3 destination, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(destination - origin) * Matrix4x4.CreateTranslation(origin);
            _drawList.Add(new GizmoGeometryInstance(_lineGeometry, world, color, hit));
        }

        /// <summary>
        /// Adds a cube to the drawlist
        /// </summary>
        /// <param name="world">World matrix of the box </param>
        /// <param name="color">The color of the box lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddCube(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance(_cubeGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a cube to the drawlist
        /// </summary>
        /// <param name="position">The 3D position of the cube</param>
        /// <param name="size">The size of the cube</param>
        /// <param name="color">The color of the box lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddCube(Vector3 position, Vector3 size, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance(_cubeGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a sphere to the drawlist
        /// </summary>
        /// <param name="position">The position of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="color">The color of the sphere lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddSphere(Vector3 position, float radius, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(radius) * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance(_sphereGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a sphere to the drawlist
        /// </summary>
        /// <param name="world">The world matrix of the sphere</param>
        /// <param name="color">The color of the sphere lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddSphere(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance(_sphereGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a cylinder to the drawlist
        /// </summary>
        /// <param name="position">The position of the cylinder</param>
        /// <param name="radius">The radius of the cylinder </param>
        /// <param name="height">The height of the cylinder </param>
        /// <param name="rotation">The quaternion rotation of the cylinder</param>
        /// <param name="color">The color of the cylinder lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddCylinder(Vector3 position, float radius, float height, Quaternion rotation, Vector3 color, bool hit = false)
        {
            var world = Matrix4x4.CreateScale(radius, height, radius)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(position);
            _drawList.Add(new GizmoGeometryInstance(_cylinderGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a cylinder to the drawlist
        /// </summary>
        /// <param name="world">The world matrix of the cylinder</param>
        /// <param name="color">The color of the cylinder lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddCylinder(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance(_cylinderGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a finite plane to the drawlist
        /// </summary>
        /// <param name="position">The position of the plane</param>
        /// <param name="normal">The normal vector of the plane</param>
        /// <param name="size">The 2D size of the plane</param>
        /// <param name="color">The color of the plane lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
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

            _drawList.Add(new GizmoGeometryInstance(_planeGeometry, world, color, hit));
        }
        /// <summary>
        /// Adds a finite plane to the drawlist
        /// </summary>
        /// <param name="world">The world matrix of the plane</param>
        /// <param name="color">The color of the plane lines</param>
        /// <param name="hit">Applies an alternative color (Default Red) </param>
        public void AddPlane(Matrix4x4 world, Vector3 color, bool hit = false)
        {
            _drawList.Add(new GizmoGeometryInstance(_planeGeometry, world, color, hit));
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

        private int[,] _frustumEdges = new int[,]
        {
            {0,1}, {1,2}, {2,3}, {3,0}, // near
            {4,5}, {5,6}, {6,7}, {7,4}, // far
            {0,4}, {1,5}, {2,6}, {3,7}  // sides
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
