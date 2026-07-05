using System.Numerics;
using System.Text.Json.Nodes;
using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering.Gizmos;

namespace Phoenix.Framework.Collisions
{
    public class OrientedBoxVolume : BoundingVolume
    {
        public Vector3 Position;
        public Vector3 Size = Vector3.One;
        public float Yaw;
        public float Pitch;
        public float Roll;

        public override BoundingVolumeType Type => BoundingVolumeType.OBB;

        public override void DrawGizmo(Gizmos gizmos)
        {
            if (!Visible) return;
            var color = Selected ? new Vector3(1, 1, 0) : new Vector3(1, 0.5f, 0);
            var obb = new OrientedBoundingBox(Position, Size);
            obb.Update(MathHelper.RotationMxFromYawPitchRoll(Yaw, Pitch, Roll));
            gizmos.AddVolume(obb, color);
        }

        public override JsonObject Serialize()
        {
            return new JsonObject
            {
                ["Type"] = "OBB",
                ["Name"] = Name,
                ["Pos_X"] = Position.X,
                ["Pos_Y"] = Position.Y,
                ["Pos_Z"] = Position.Z,
                ["Size_X"] = Size.X,
                ["Size_Y"] = Size.Y,
                ["Size_Z"] = Size.Z,
                ["Yaw"] = Yaw,
                ["Pitch"] = Pitch,
                ["Roll"] = Roll,
            };
        }

        public new static OrientedBoxVolume Deserialize(JsonObject data)
        {
            return new OrientedBoxVolume
            {
                Name = (string)data["Name"]!,
                Position = new Vector3((float)data["Pos_X"]!, (float)data["Pos_Y"]!, (float)data["Pos_Z"]!),
                Size = new Vector3((float)data["Size_X"]!, (float)data["Size_Y"]!, (float)data["Size_Z"]!),
                Yaw = (float)data["Yaw"]!,
                Pitch = (float)data["Pitch"]!,
                Roll = (float)data["Roll"]!,
            };
        }
    }
}
