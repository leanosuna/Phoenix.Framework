using Silk.NET.Core.Native;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Marshals a list of managed strings into a contiguous unmanaged UTF-8 array for Vulkan create info.
/// </summary>
internal sealed unsafe class MarshaledStringArray : IDisposable
{
    private readonly byte** _pointer;
    private readonly int _length;

    public byte** Pointer => _pointer;

    /// <summary>
    /// Allocates and converts managed strings into a null-terminated UTF-8 byte pointer array.
    /// </summary>
    public MarshaledStringArray(IReadOnlyList<string> strings)
    {
        _length = strings.Count;
        _pointer = (byte**)Marshal.AllocHGlobal(sizeof(byte*) * _length);
        for (int i = 0; i < _length; i++)
        {
            _pointer[i] = (byte*)SilkMarshal.StringToPtr(strings[i]);
        }
    }

    /// <summary>
    /// Frees all allocated UTF-8 string pointers and the outer pointer array.
    /// </summary>
    public void Dispose()
    {
        for (int i = 0; i < _length; i++)
        {
            SilkMarshal.Free((nint)_pointer[i]);
        }
        Marshal.FreeHGlobal((nint)_pointer);
    }
}
