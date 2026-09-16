using System.Runtime.InteropServices;
using Compressonator.NET;

namespace Phoenix.Framework.AssetImport.Native;

/// <summary>
/// Direct P/Invoke bindings for AMD Compressonator native library.
/// Bypasses buggy managed wrappers to guarantee safe memory handling and eliminate finalizer collisions.
/// </summary>
internal static class CompressonatorNativeMethods
{
    private const string LibName = "CMP_Compressonator";

    [DllImport(LibName, EntryPoint = "CMP_CalculateBufferSize")]
    public static extern uint CMP_CalculateBufferSize(CMP_Texture texture);

    [DllImport(LibName, EntryPoint = "CMP_ConvertTexture")]
    public static extern CMP_ERROR CMP_ConvertTexture(
        CMP_Texture sourceTexture,
        CMP_Texture destTexture,
        IntPtr options,
        IntPtr feedbackProc);
}
