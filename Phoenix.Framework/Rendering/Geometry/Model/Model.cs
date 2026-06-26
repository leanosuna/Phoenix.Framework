using Phoenix.Framework.Rendering.Geometry.Model.Meshes;
using Phoenix.Framework.Rendering.Shaders;
using System.Numerics;

namespace Phoenix.Framework.Rendering.Geometry.Model
{
    public class Model
    {
        public List<ModelPart> Parts { get; internal set; } = default!;
        public List<string> TextureNames { get; internal set; } = default!;
        public bool HasTextures => TextureNames.Count > 0;

        public Model()
        {

        }

        public void Draw()
        {
            foreach(var part in Parts)
            {
                foreach(var mesh in part.Meshes)
                {
                    mesh.Draw();
                }
            }
        }

        public void Draw(ShaderHelper shader)
        {
            shader.Use();
            foreach (var part in Parts)
            {
                foreach (var mesh in part.Meshes)
                {
                    mesh.Draw();
                }
            }
        }

        public void Draw(ShaderHelper shader, Action<int, string, int, string, Matrix4x4> perElement)
        {
            shader.Use();
            for(var p = 0; p < Parts.Count; p++)
            {
                var part = Parts[p];
                for(var m = 0; m < part.Meshes.Count; m++)
                {
                    var mesh = part.Meshes[m];
                    perElement(p, part.Name, m, mesh.Name, mesh.Transform);
                    mesh.Draw();
                }
            }
        }


        public T[][] GetVertexData<T>() where T : unmanaged
        {
            List<T[]> result = new();
            
            foreach(var part in Parts)
            {
                foreach(var mesh in part.Meshes)
                {
                    result.Add(mesh.GetVertexData<T>());
                }
            }

            return result.ToArray();
        }
            
    }
}
