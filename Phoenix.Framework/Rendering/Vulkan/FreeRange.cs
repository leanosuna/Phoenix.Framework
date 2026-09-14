namespace Phoenix.Framework.Rendering.Vulkan;

/// <summary>
/// Represents a contiguous range of available bindless descriptor slots.
/// </summary>
internal readonly record struct FreeRange(uint Start, uint Count);
