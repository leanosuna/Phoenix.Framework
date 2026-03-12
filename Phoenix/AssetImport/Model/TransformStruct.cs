using Phoenix.Rendering.Animation;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetImport.Model
{
    public struct TransformStruct
    {
        public Vector3 Scale { get; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Translation { get; }

        public TransformStruct(Vector3 scale, Quaternion rotation, Vector3 translation)
        {
            Scale = scale;
            Rotation = rotation;
            Translation = translation;
        }
    }
}
