namespace Phoenix.Rendering.Geometry.Model
{
    public class Model
    {
        public List<ModelPart> Parts { get; internal set; } = default!;
        public List<string> TextureNames { get; internal set; } = default!;
        public bool HasTextures => TextureNames.Count > 0;

        public Model()
        {

        }

    }
}
