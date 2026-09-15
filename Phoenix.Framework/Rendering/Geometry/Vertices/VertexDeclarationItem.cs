namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    public class VertexDeclarationItem
    {
        public int SizeBytes { get; private set; }
        public int Size { get; private set; }
        public bool IsInt { get; private set; }
        internal VertexDeclarationItem(VertexAttributeType type)
        {
            SizeBytes = type.SizeInBytes();
            Size = type.Size();
            IsInt = type.IsInt();
        }
    }
}
