using Phoenix.Framework.Rendering.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.Framework.Rendering.Geometry.Model
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

        public T[][] GetVertexData<T>() where T : unmanaged 
            => Meshes.Select(m => m.GetVertexData<T>()).ToArray();
    }
}
