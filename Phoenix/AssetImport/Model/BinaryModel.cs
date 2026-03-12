using Phoenix.AssetImport.Model.Animation;
using Phoenix.Rendering.Animation;
using Phoenix.Rendering.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetImport.Model
{
    public class BinaryModel
    {
        public List<BinaryModelPart> Parts { get; internal set; } = default!;
        public List<string> TextureNames { get; internal set; } = default!;
        public bool HasTextures => TextureNames.Count > 0;

        public BinaryModel()
        {

        }

    }
}
