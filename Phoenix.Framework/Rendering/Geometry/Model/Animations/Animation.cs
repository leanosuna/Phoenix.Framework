using Phoenix.Framework.Rendering.Geometry;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model.Animations
{
    public class Animation
    {
        public string Name { get; private set; } = "";
        public float Duration { get; private set; }
        public float TicksPerSecond { get; private set; }

        private Keyframe[][] _keyFrames = default!;

        private float _randomStartOffset;
        private float _currentTime;
        private int _boneCount;
        public Animation(string name, float duration, float tps, Keyframe[][] keyFrames)
        {
            Name = name;
            Duration = duration;
            _keyFrames = keyFrames;
            TicksPerSecond = tps;
            if (TicksPerSecond <= 0)
                TicksPerSecond = 25.0f;

            _boneCount = _keyFrames.GetLength(0);
            _randomStartOffset = (float)new Random().NextDouble() * Duration;
        }
        public void Reset()
        {
            _currentTime = _randomStartOffset;
        }

        public void Update(float deltaTime, Matrix4x4[] finalBoneMatrices)
        {
            _currentTime += TicksPerSecond * deltaTime;
            _currentTime %= Duration;

            for (int b = 0; b < _boneCount; b++)
            {
                var keys = _keyFrames[b];
                if (keys.Length == 0)
                {
                    finalBoneMatrices[b] = Matrix4x4.Identity;
                    continue;
                }

                var i0 = GetStartingIndex(_currentTime, keys);
                var i1 = Math.Min(i0 + 1, keys.Length - 1);

                var interpolated = Interpolate(keys[i0], keys[i1], _currentTime);

                //var M = interpolated.AsMatrix();
                //finalBoneMatrices[b] = Matrix4x4.Transpose(M);

                finalBoneMatrices[b] = interpolated.AsMatrix();
            }
        }

        private int GetStartingIndex(float time, Keyframe[] keys)
        {
            if (keys.Length == 0) return 0;
            for (int index = 0; index < keys.Length - 1; index++)
            {
                if (time < keys[index + 1].TimeStamp)
                    return index;
            }
            return Math.Max(0, keys.Length - 2);
        }

        private Transform Interpolate(Keyframe current, Keyframe next, float time)
        {
            var lerpFactor = 0.0f;
            var midWayLength = time - current.TimeStamp;
            var framesDiff = next.TimeStamp - current.TimeStamp;
            lerpFactor = midWayLength / framesDiff;

            return current.Interpolate(next, lerpFactor);
        }

    }
}
