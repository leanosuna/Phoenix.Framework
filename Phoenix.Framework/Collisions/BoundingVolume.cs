using System.Numerics;
using System.Text.Json.Nodes;
using Phoenix.Framework.Rendering.Gizmos;

namespace Phoenix.Framework.Collisions
{
    public enum BoundingVolumeType
    {
        AABB,
        OBB,
        Cylinder,
        Sphere,
        Capsule,
    }

    public abstract class BoundingVolume
    {
        public string Name = "";
        public bool Visible = true;
        public bool Selected;

        public abstract BoundingVolumeType Type { get; }

        public abstract void DrawGizmo(Gizmos gizmos);

        public abstract JsonObject Serialize();

        public static BoundingVolume Deserialize(JsonObject data)
        {
            var type = (string)data["Type"]!;
            return type switch
            {
                "AABB" => AxisAlignedBoxVolume.Deserialize(data),
                "OBB" => OrientedBoxVolume.Deserialize(data),
                "Cylinder" => CylinderVolume.Deserialize(data),
                "Sphere" => SphereVolume.Deserialize(data),
                "Capsule" => CapsuleVolume.Deserialize(data),
                _ => throw new ArgumentException($"Unknown volume type: {type}"),
            };
        }
    }
}
