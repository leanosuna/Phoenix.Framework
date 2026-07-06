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

    public abstract class SerializableVolume
    {
        public string Name = "";
        public bool Visible = true;
        public bool Selected;

        public abstract BoundingVolumeType Type { get; }

        public abstract void DrawGizmo(Gizmos gizmos);

        public abstract JsonObject Serialize();

        public static SerializableVolume Deserialize(JsonObject data)
        {
            var type = (string)data["Type"]!;
            return type switch
            {
                "AABB" => SerializableAABB.Deserialize(data),
                "OBB" => SerializableOBB.Deserialize(data),
                "Cylinder" => SerializableCylinder.Deserialize(data),
                "Sphere" => SerializableSphere.Deserialize(data),
                "Capsule" => SerializableCapsule.Deserialize(data),
                _ => throw new ArgumentException($"Unknown volume type: {type}"),
            };
        }
    }
}
