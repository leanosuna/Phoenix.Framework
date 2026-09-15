using System.Numerics;
using System.Text.Json.Nodes;
using Phoenix.Framework.Rendering.Gizmos;

namespace Phoenix.Framework.Collisions
{
    public class SerializableCapsule : SerializableVolume
    {
        public Vector3 PointA;
        public Vector3 PointB;
        public float Radius = 0.5f;

        public override BoundingVolumeType Type => BoundingVolumeType.Capsule;

        public override void DrawGizmo(Gizmos gizmos)
        {
            if (!Visible) return;
            var color = Selected ? new Vector3(1, 1, 0) : new Vector3(1, 0.5f, 0.5f);
            gizmos.AddCapsule(PointA, PointB, Radius, color);
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["Type"] = "Capsule",
                ["Name"] = Name,
                ["A_X"] = PointA.X,
                ["A_Y"] = PointA.Y,
                ["A_Z"] = PointA.Z,
                ["B_X"] = PointB.X,
                ["B_Y"] = PointB.Y,
                ["B_Z"] = PointB.Z,
                ["Radius"] = Radius,
            };
        }

        public new static SerializableCapsule Deserialize(JsonObject data)
        {
            return new SerializableCapsule
            {
                Name = (string)data["Name"]!,
                PointA = new Vector3((float)data["A_X"]!, (float)data["A_Y"]!, (float)data["A_Z"]!),
                PointB = new Vector3((float)data["B_X"]!, (float)data["B_Y"]!, (float)data["B_Z"]!),
                Radius = (float)data["Radius"]!,
            };
        }

        public static implicit operator Capsule(SerializableCapsule v)
            => new(v.PointA, v.PointB, v.Radius);
    }
}
