using System.Numerics;

namespace Phoenix.Rendering.Geometry.Model.Animations
{
    public class Keyframe
    {
        public float TimeStamp { get; }
        public Transform Transform { get; }
        public Keyframe(float timeStamp, Vector3 scale, Quaternion rotation, Vector3 position)
        {
            TimeStamp = timeStamp;
            Transform = new Transform(scale, rotation, position);
        }
        public Keyframe(float timeStamp, TransformStruct transform)
        {
            TimeStamp = timeStamp;
            Transform = new Transform(transform.Scale, transform.Rotation, transform.Translation);
        }

        public Transform Interpolate(Keyframe other, float factor)
        {
            return Transform.Interpolate(other.Transform, factor);
        }
    }
}
