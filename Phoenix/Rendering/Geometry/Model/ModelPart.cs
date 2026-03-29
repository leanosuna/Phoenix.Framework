using Phoenix.Rendering.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Rendering.Geometry.Model
{
    public class ModelPart
    {
        public string Name { get; private set; }
        public List<ModelMesh> Meshes { get; private set; }

        public ModelPart(string name, List<ModelMesh> meshes)
        {
            Name = name;
            Meshes = meshes;
        }
    }
}
