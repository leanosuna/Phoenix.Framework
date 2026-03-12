using Phoenix.Rendering.Animation;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetImport.Model.Animation
{
    public class BinaryAnimation
    {
        public string Name { get; private set; } = "";
        public float Duration { get; private set; }
        public float TicksPerSecond { get; private set; }
        public Transform[] CurrentFrame { get; private set; } = default!;
        public Matrix4x4[] Transforms { get; private set; } = default!;
        
        private Keyframe[][] _keyFrames = default!;

        private float _randomStartOffset;
        private float _currentTime;
        private int _boneCount;
        public BinaryAnimation(string name, float duration, float tps, Keyframe[][] keyFrames)
        {
            Name = name;
            Duration = duration;
            _keyFrames = keyFrames;
            TicksPerSecond = tps;
            if (TicksPerSecond <= 0)
                TicksPerSecond = 25.0f;

            _boneCount = _keyFrames.GetLength(0);
            CurrentFrame = new Transform[_boneCount];
            Transforms = new Matrix4x4[_boneCount];
            for (int i = 0; i < _boneCount; i++)
            {
                CurrentFrame[i] = new Transform(Vector3.One, Quaternion.Identity, Vector3.Zero);
                Transforms[i] = Matrix4x4.Identity;
            }
            _randomStartOffset = (float)new Random().NextDouble() * Duration;
        }
        public void Reset()
        {
            _currentTime = _randomStartOffset;
        }

        public void UpdateFrameSRT(float deltaTime)
        {
            _currentTime += TicksPerSecond * deltaTime;
            _currentTime %= Duration;

            for (int b = 0; b < _boneCount; b++)
            {
                var keys = _keyFrames[b];
                if (keys.Length == 0)
                {
                    Transforms[b] = Matrix4x4.Identity;
                    continue;
                }

                var i0 = GetStartingIndex(_currentTime, keys);
                var i1 = Math.Min(i0 + 1, keys.Length - 1);

                CurrentFrame[b] = Interpolate(keys[i0], keys[i1], _currentTime);
            }

        }
        public void Update(float deltaTime)
        {
            UpdateFrameSRT(deltaTime);

            for (int b = 0; b < _boneCount; b++)
            {
                var M = Matrix4x4.CreateScale(CurrentFrame[b].Scale)
                       * Matrix4x4.CreateFromQuaternion(CurrentFrame[b].Rotation)
                       * Matrix4x4.CreateTranslation(CurrentFrame[b].Translation);

                Transforms[b] = M;
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
