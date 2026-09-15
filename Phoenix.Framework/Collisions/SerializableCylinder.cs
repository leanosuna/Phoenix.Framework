using System.Numerics;
using System.Text.Json.Nodes;
using Phoenix.Framework.Rendering.Gizmos;

namespace Phoenix.Framework.Collisions
{
    public class SerializableCylinder : SerializableVolume
    {
        public Vector3 Position;
        public float Radius = 1f;
        public float Height = 2f;
        public float Rotation;

        public override BoundingVolumeType Type => BoundingVolumeType.Cylinder;

        public override void DrawGizmo(Gizmos gizmos)
        {
            if (!Visible) return;
            var color = Selected ? new Vector3(1, 1, 0) : new Vector3(0, 1, 0.5f);
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rotation);
            gizmos.AddCylinder(Position, Radius, Height, q, color);
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["Type"] = "Cylinder",
                ["Name"] = Name,
                ["Pos_X"] = Position.X,
                ["Pos_Y"] = Position.Y,
                ["Pos_Z"] = Position.Z,
                ["Radius"] = Radius,
                ["Height"] = Height,
                ["Rotation"] = Rotation,
            };
        }

        public new static SerializableCylinder Deserialize(JsonObject data)
        {
            return new SerializableCylinder
            {
                Name = (string)data["Name"]!,
                Position = new Vector3((float)data["Pos_X"]!, (float)data["Pos_Y"]!, (float)data["Pos_Z"]!),
                Radius = (float)data["Radius"]!,
                Height = (float)data["Height"]!,
                Rotation = (float)data["Rotation"]!,
            };
        }

        public BoundingCylinder ToCylinder()
        {
            var cyl = new BoundingCylinder(Position, Radius, Height);
            if (Rotation != 0)
                cyl.Update(Matrix4x4.CreateRotationY(Rotation));
            return cyl;
        }

        public static implicit operator BoundingCylinder(SerializableCylinder v)
            => v.ToCylinder();
    }
}
