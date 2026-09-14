using System.Runtime.InteropServices;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Extension methods for writing unmanaged structs and arrays directly to a BinaryWriter.
/// </summary>
public static class BinaryWriterTools
{
    public static int Write<T>(this BinaryWriter bw, T[] value) where T : unmanaged
    {
        var spanT = value.AsSpan();
        var span = MemoryMarshal.AsBytes(spanT);
        bw.Write(span);
        return span.Length;
    }

    public static int Write<T>(this BinaryWriter bw, ReadOnlySpan<T> value) where T : unmanaged
    {
        var span = MemoryMarshal.AsBytes(value);
        bw.Write(span);
        return span.Length;
    }

    public static int Write<T>(this BinaryWriter bw, T value) where T : unmanaged
    {
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        bw.Write(span);
        return span.Length;
    }
}
