using System.Numerics;
using System.Text.Json.Nodes;
using Phoenix.Framework.Rendering.Gizmos;

namespace Phoenix.Framework.Collisions
{
    public class SphereVolume : BoundingVolume
    {
        public Vector3 Position;
        public float Radius = 1f;

        public override BoundingVolumeType Type => BoundingVolumeType.Sphere;

        public override void DrawGizmo(Gizmos gizmos)
        {
            if (!Visible) return;
            var color = Selected ? new Vector3(1, 1, 0) : new Vector3(1, 0, 0.5f);
            var sphere = new BoundingSphere(Position, Radius);
            gizmos.AddVolume(sphere, color);
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["Type"] = "Sphere",
                ["Name"] = Name,
                ["Pos_X"] = Position.X,
                ["Pos_Y"] = Position.Y,
                ["Pos_Z"] = Position.Z,
                ["Radius"] = Radius,
            };
        }

        public new static SphereVolume Deserialize(JsonObject data)
        {
            return new SphereVolume
            {
                Name = (string)data["Name"]!,
                Position = new Vector3((float)data["Pos_X"]!, (float)data["Pos_Y"]!, (float)data["Pos_Z"]!),
                Radius = (float)data["Radius"]!,
            };
        }
    }
}
