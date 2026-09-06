using Silk.NET.Vulkan;
using System.Diagnostics;

namespace Phoenix.Framework.Rendering.Vulkan;

internal static class VulkanHelper
{
    /// <summary>
    /// Checks a Vulkan API result and throws an exception on failure.
    /// </summary>
    [DebuggerHidden]
    public static void Check(Result result, string message)
    {
        if (result != Result.Success)
            throw new InvalidOperationException($"[Vulkan Error] {message}: {result}");
    }
}
