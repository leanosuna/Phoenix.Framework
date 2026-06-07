using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Geometry.Vertices
{
    public class VertexBuilder
    {
        private byte[] _vertexData;
        private int _bytesWritten;
        private int _currentIndex;

        private VertexDeclaration _vertexDeclaration;
        
        public VertexBuilder(VertexDeclaration vertexDeclaration)
        {
            _vertexDeclaration = vertexDeclaration;
            _vertexData = new byte[_vertexDeclaration.StrideBytes];
        }

        public void Reset()
        {
            _bytesWritten = 0;
            _currentIndex = 0;
        }
        
        public unsafe VertexBuilder Add<T>(T data)
            where T : unmanaged
        {
            var size = sizeof(T);
            var decSize = _vertexDeclaration.VertexItems[_currentIndex].SizeBytes;
            if (decSize != size)
                throw new Exception($"expected {decSize}, got {size} at index {_currentIndex}");
                        
            MemoryMarshal.Write(_vertexData.AsSpan(_bytesWritten),in data);
                        
            _bytesWritten += size;
            _currentIndex++;
            return this;
        }

        public ReadOnlySpan<byte> Build()
        {
            if (_currentIndex != _vertexDeclaration.VertexItems.Count)
                throw new Exception(
                    $"vertex incomplete: expected {_vertexDeclaration.VertexItems.Count} attributes, got {_currentIndex}");

            return _vertexData;
        }
    }
}
