using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Phoenix.Framework.Rendering.Vulkan;

public sealed unsafe class VulkanContext : IDisposable
{

    private readonly Vk _vk;
    private readonly Instance _instance;
    private readonly KhrSurface _khrSurface;
    private readonly SurfaceKHR _surface;
    private readonly PhysicalDevice _physicalDevice;
    private readonly PhysicalDeviceProperties _physicalDeviceProperties;
    private readonly Device _device;
    private readonly KhrSwapchain _khrSwapchain;
    private readonly Queue _graphicsQueue;
    private readonly uint _graphicsFamilyIndex;
    private readonly Queue _presentQueue;
    private readonly uint _presentFamilyIndex;
    private readonly Queue? _transferQueue;
    private readonly uint? _transferFamilyIndex;
    private readonly ExtDebugUtils? _extDebugUtils;
    private readonly DebugUtilsMessengerEXT _debugMessenger;
    private CommandPool _utilityCommandPool;
    private bool _disposed;

    public Vk Vk => _vk;
    public Instance Instance => _instance;
    public KhrSurface KhrSurface => _khrSurface;
    public SurfaceKHR Surface => _surface;
    public PhysicalDevice PhysicalDevice => _physicalDevice;
    public PhysicalDeviceProperties PhysicalDeviceProperties => _physicalDeviceProperties;
    public Device Device => _device;
    public KhrSwapchain KhrSwapchain => _khrSwapchain;
    public Queue GraphicsQueue => _graphicsQueue;
    public uint GraphicsFamilyIndex => _graphicsFamilyIndex;
    public Queue PresentQueue => _presentQueue;
    public uint PresentFamilyIndex => _presentFamilyIndex;
    public Queue? TransferQueue => _transferQueue;
    public uint? TransferFamilyIndex => _transferFamilyIndex;

    /// <summary>
    /// Initializes the core Vulkan context, creating the instance, surface, physical device, and logical device.
    /// </summary>
    public VulkanContext(IWindow window, string applicationName = "Phoenix Game", bool enableValidation = true)
    {
        _vk = Vk.GetApi();
        _instance = CreateInstance(window, applicationName, enableValidation, out _extDebugUtils, out _debugMessenger);

        if (!_vk.TryGetInstanceExtension(_instance, out _khrSurface))
            throw new NotSupportedException("Vulkan surface extension (VK_KHR_surface) is not available.");

        var windowSurface = (window as IVkSurface) ?? window.VkSurface
            ?? throw new NotSupportedException("Window does not implement IVkSurface.");
        _surface = windowSurface.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();

        _physicalDevice = SelectPhysicalDevice(_instance, _surface,
            out _physicalDeviceProperties,
            out _graphicsFamilyIndex, out _presentFamilyIndex, out _transferFamilyIndex);

        _device = CreateLogicalDevice(_physicalDevice,
            _graphicsFamilyIndex, _presentFamilyIndex, _transferFamilyIndex,
            out _graphicsQueue, out _presentQueue, out _transferQueue);

        if (!_vk.TryGetDeviceExtension(_instance, _device, out _khrSwapchain))
            throw new NotSupportedException("Vulkan swapchain extension (VK_KHR_swapchain) is not available on selected device.");

        CreateUtilityCommandPool();
    }

    /// <summary>
    /// Creates the Vulkan instance with required windowing extensions, portability support, and optional validation layers.
    /// </summary>
    private Instance CreateInstance(IWindow window, string applicationName, bool enableValidation,
        out ExtDebugUtils? debugUtils, out DebugUtilsMessengerEXT debugMessenger)
    {
        debugUtils = null;
        debugMessenger = default;

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr(applicationName),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)SilkMarshal.StringToPtr("Phoenix Framework"),
            EngineVersion = new Version32(2, 0, 0),
            ApiVersion = Vk.Version13
        };

        var availableInstanceExtensions = GetAvailableInstanceExtensions();
        List<string> requiredExtensions = [];

        var windowSurface = (window as IVkSurface) ?? window.VkSurface
            ?? throw new InvalidOperationException("Window has no Vulkan surface provider.");
        byte** windowExts = windowSurface.GetRequiredExtensions(out uint windowExtCount);
        for (int i = 0; i < windowExtCount; i++)
        {
            requiredExtensions.Add(SilkMarshal.PtrToString((nint)windowExts[i])!);
        }

        bool hasPortabilityEnumeration = availableInstanceExtensions.Contains("VK_KHR_portability_enumeration");
        if (hasPortabilityEnumeration)
            requiredExtensions.Add("VK_KHR_portability_enumeration");

        List<string> enabledLayers = [];
        bool validationActive = false;

#if DEBUG
        if (enableValidation)
        {
            var availableLayers = GetAvailableInstanceLayers();
            if (availableLayers.Contains("VK_LAYER_KHRONOS_validation"))
            {
                enabledLayers.Add("VK_LAYER_KHRONOS_validation");
                validationActive = true;
                if (availableInstanceExtensions.Contains(ExtDebugUtils.ExtensionName))
                    requiredExtensions.Add(ExtDebugUtils.ExtensionName);
            }
        }
#endif

        InstanceCreateFlags createFlags = hasPortabilityEnumeration
            ? InstanceCreateFlags.EnumeratePortabilityBitKhr
            : InstanceCreateFlags.None;

        using var marshaledExtensions = new MarshaledStringArray([.. requiredExtensions]);
        using var marshaledLayers = new MarshaledStringArray([.. enabledLayers]);

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            Flags = createFlags,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)requiredExtensions.Count,
            PpEnabledExtensionNames = marshaledExtensions.Pointer,
            EnabledLayerCount = (uint)enabledLayers.Count,
            PpEnabledLayerNames = marshaledLayers.Pointer
        };

        VulkanHelper.Check(_vk.CreateInstance(in createInfo, null, out var instance), "Failed to create Vulkan instance.");

        SilkMarshal.Free((nint)appInfo.PApplicationName);
        SilkMarshal.Free((nint)appInfo.PEngineName);

        if (validationActive && _vk.TryGetInstanceExtension(instance, out debugUtils))
            debugMessenger = SetupDebugMessenger(debugUtils!, instance);

        return instance;
    }

    /// <summary>
    /// Configures the Vulkan debug messenger callback for validation messages.
    /// </summary>
    private static DebugUtilsMessengerEXT SetupDebugMessenger(ExtDebugUtils debugUtils, Instance instance)
    {
        DebugUtilsMessengerCreateInfoEXT createInfo = new()
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                          DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                          DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback)
        };

        VulkanHelper.Check(debugUtils.CreateDebugUtilsMessenger(instance, in createInfo, null, out var messenger),
            "Failed to create Vulkan debug messenger.");
        return messenger;
    }

    /// <summary>
    /// Handles callbacks from the Vulkan validation layers and routes them to framework logging.
    /// </summary>
    private static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData)
    {
        string message = SilkMarshal.PtrToString((nint)pCallbackData->PMessage) ?? "Unknown Vulkan debug message";
        switch (messageSeverity)
        {
            case DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt:
                Log.Error($"[Vulkan Validation Error] {message}");
                break;
            case DebugUtilsMessageSeverityFlagsEXT.WarningBitExt:
                Log.Warn($"[Vulkan Validation Warning] {message}");
                break;
            default:
                Log.Info($"[Vulkan Validation] {message}");
                break;
        }
        return Vk.False;
    }

    /// <summary>
    /// Selects the best physical device supporting Vulkan 1.3, dynamic rendering, and swapchain presentation.
    /// </summary>
    private PhysicalDevice SelectPhysicalDevice(Instance instance, SurfaceKHR surface,
        out PhysicalDeviceProperties selectedProperties,
        out uint graphicsFamily, out uint presentFamily, out uint? transferFamily)
    {
        var devices = _vk.GetPhysicalDevices(instance);
        if (devices.Count == 0)
            throw new NotSupportedException("No Vulkan-capable physical devices found.");

        PhysicalDevice? bestDevice = null;
        PhysicalDeviceProperties bestProperties = default;
        uint bestGraphics = 0;
        uint bestPresent = 0;
        uint? bestTransfer = null;
        int bestScore = -1;

        foreach (var device in devices)
        {
            _vk.GetPhysicalDeviceProperties(device, out var props);

            if (props.ApiVersion < Vk.Version13)
                continue;

            if (!FindQueueFamilies(device, surface, out var gfx, out var pres, out var xfer))
                continue;

            if (!CheckDeviceExtensionSupport(device))
                continue;

            if (!CheckDeviceFeatureSupport(device))
                continue;

            int score = props.DeviceType switch
            {
                PhysicalDeviceType.DiscreteGpu => 1000,
                PhysicalDeviceType.IntegratedGpu => 500,
                PhysicalDeviceType.VirtualGpu => 250,
                PhysicalDeviceType.Cpu => 100,
                _ => 10
            };

            if (gfx == pres)
                score += 50;

            if (score > bestScore)
            {
                bestScore = score;
                bestDevice = device;
                bestProperties = props;
                bestGraphics = gfx;
                bestPresent = pres;
                bestTransfer = xfer;
            }
        }

        if (bestDevice is null)
            throw new NotSupportedException("No suitable Vulkan 1.3 physical device found with dynamic rendering support.");

        selectedProperties = bestProperties;
        graphicsFamily = bestGraphics;
        presentFamily = bestPresent;
        transferFamily = bestTransfer;
        PhysicalDeviceProperties* pProps = &bestProperties;
        Log.Info($"[Vulkan] Selected Device: {SilkMarshal.PtrToString((nint)pProps->DeviceName)} ({bestProperties.DeviceType})");
        return bestDevice.Value;
    }

    /// <summary>
    /// Searches the physical device for graphics, present, and optional transfer queue families.
    /// </summary>
    private bool FindQueueFamilies(PhysicalDevice device, SurfaceKHR surface,
        out uint graphicsFamily, out uint presentFamily, out uint? transferFamily)
    {
        graphicsFamily = 0;
        presentFamily = 0;
        transferFamily = null;

        bool hasGraphics = false;
        bool hasPresent = false;

        uint count = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        QueueFamilyProperties[] queueFamilies = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, pQueueFamilies);
        }

        for (uint i = 0; i < count; i++)
        {
            var family = queueFamilies[i];

            if ((family.QueueFlags & QueueFlags.GraphicsBit) != 0 && !hasGraphics)
            {
                graphicsFamily = i;
                hasGraphics = true;
            }

            _khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);
            if ((bool)presentSupport)
            {
                if (!hasPresent)
                {
                    presentFamily = i;
                    hasPresent = true;
                }
                else if (hasGraphics && i == graphicsFamily)
                {
                    presentFamily = i;
                }
            }

            if ((family.QueueFlags & QueueFlags.TransferBit) != 0 && (family.QueueFlags & QueueFlags.GraphicsBit) == 0 && transferFamily is null)
            {
                transferFamily = i;
            }
        }

        return hasGraphics && hasPresent;
    }

    /// <summary>
    /// Checks that the physical device supports required device extensions such as VK_KHR_swapchain.
    /// </summary>
    private bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        var availableExtensions = GetAvailableDeviceExtensions(device);
        return availableExtensions.Contains(KhrSwapchain.ExtensionName);
    }

    /// <summary>
    /// Verifies that the physical device supports Vulkan 1.3 dynamic rendering, synchronization2, and Vulkan 1.2 descriptor indexing.
    /// </summary>
    private bool CheckDeviceFeatureSupport(PhysicalDevice device)
    {
        PhysicalDeviceVulkan12Features features12 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan12Features
        };

        PhysicalDeviceVulkan13Features features13 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            PNext = &features12
        };

        PhysicalDeviceFeatures2 features2 = new()
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &features13
        };

        _vk.GetPhysicalDeviceFeatures2(device, &features2);
        return features2.Features.SamplerAnisotropy &&
               features13.DynamicRendering &&
               features13.Synchronization2 &&
               features12.DescriptorBindingPartiallyBound &&
               features12.RuntimeDescriptorArray &&
               features12.DescriptorBindingSampledImageUpdateAfterBind &&
               features12.ShaderSampledImageArrayNonUniformIndexing &&
               features12.DescriptorIndexing;
    }

    /// <summary>
    /// Creates the logical device with graphics, present, and transfer queues, enabling dynamic rendering.
    /// </summary>
    private Device CreateLogicalDevice(PhysicalDevice physicalDevice,
        uint graphicsFamily, uint presentFamily, uint? transferFamily,
        out Queue graphicsQueue, out Queue presentQueue, out Queue? transferQueue)
    {
        HashSet<uint> uniqueFamilies = [graphicsFamily, presentFamily];
        if (transferFamily is uint xfer)
            uniqueFamilies.Add(xfer);

        float priority = 1.0f;
        DeviceQueueCreateInfo[] queueCreateInfos = new DeviceQueueCreateInfo[uniqueFamilies.Count];
        int queueIndex = 0;
        foreach (uint family in uniqueFamilies)
        {
            queueCreateInfos[queueIndex++] = new DeviceQueueCreateInfo()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = family,
                QueueCount = 1,
                PQueuePriorities = &priority
            };
        }

        List<string> enabledExtensions = [KhrSwapchain.ExtensionName];
        var availableExtensions = GetAvailableDeviceExtensions(physicalDevice);
        if (availableExtensions.Contains("VK_KHR_portability_subset"))
            enabledExtensions.Add("VK_KHR_portability_subset");

        PhysicalDeviceVulkan12Features features12 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan12Features,
            DescriptorBindingPartiallyBound = true,
            RuntimeDescriptorArray = true,
            DescriptorBindingSampledImageUpdateAfterBind = true,
            ShaderSampledImageArrayNonUniformIndexing = true,
            DescriptorIndexing = true
        };

        PhysicalDeviceVulkan13Features features13 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            PNext = &features12,
            DynamicRendering = true,
            Synchronization2 = true
        };

        PhysicalDeviceFeatures features10 = new()
        {
            SamplerAnisotropy = true
        };

        PhysicalDeviceFeatures2 features2 = new()
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &features13,
            Features = features10
        };

        using var marshaledExtensions = new MarshaledStringArray([.. enabledExtensions]);

        fixed (DeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos)
        {
            DeviceCreateInfo createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                PNext = &features2,
                QueueCreateInfoCount = (uint)queueCreateInfos.Length,
                PQueueCreateInfos = pQueueCreateInfos,
                EnabledExtensionCount = (uint)enabledExtensions.Count,
                PpEnabledExtensionNames = marshaledExtensions.Pointer
            };

            VulkanHelper.Check(_vk.CreateDevice(physicalDevice, in createInfo, null, out var device),
                "Failed to create Vulkan logical device.");

            _vk.GetDeviceQueue(device, graphicsFamily, 0, out graphicsQueue);
            _vk.GetDeviceQueue(device, presentFamily, 0, out presentQueue);

            transferQueue = transferFamily switch
            {
                uint family => _vk.GetDeviceQueue(device, family, 0),
                _ => null
            };

            return device;
        }
    }

    /// <summary>
    /// Retrieves all available Vulkan instance extension names.
    /// </summary>
    private HashSet<string> GetAvailableInstanceExtensions()
    {
        uint count = 0;
        _vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, null);
        ExtensionProperties[] extensions = new ExtensionProperties[count];
        fixed (ExtensionProperties* pExtensions = extensions)
        {
            _vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, pExtensions);
        }

        HashSet<string> names = [];
        for (int i = 0; i < count; i++)
        {
            fixed (byte* namePtr = extensions[i].ExtensionName)
            {
                names.Add(SilkMarshal.PtrToString((nint)namePtr)!);
            }
        }
        return names;
    }

    /// <summary>
    /// Retrieves all available Vulkan instance layer names.
    /// </summary>
    private HashSet<string> GetAvailableInstanceLayers()
    {
        uint count = 0;
        _vk.EnumerateInstanceLayerProperties(ref count, null);
        LayerProperties[] layers = new LayerProperties[count];
        fixed (LayerProperties* pLayers = layers)
        {
            _vk.EnumerateInstanceLayerProperties(ref count, pLayers);
        }

        HashSet<string> names = [];
        for (int i = 0; i < count; i++)
        {
            fixed (byte* namePtr = layers[i].LayerName)
            {
                names.Add(SilkMarshal.PtrToString((nint)namePtr)!);
            }
        }
        return names;
    }

    /// <summary>
    /// Retrieves all available device extension names for a physical device.
    /// </summary>
    private HashSet<string> GetAvailableDeviceExtensions(PhysicalDevice device)
    {
        uint count = 0;
        _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, null);
        ExtensionProperties[] extensions = new ExtensionProperties[count];
        fixed (ExtensionProperties* pExtensions = extensions)
        {
            _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, pExtensions);
        }

        HashSet<string> names = [];
        for (int i = 0; i < count; i++)
        {
            fixed (byte* namePtr = extensions[i].ExtensionName)
            {
                names.Add(SilkMarshal.PtrToString((nint)namePtr)!);
            }
        }
        return names;
    }

    /// <summary>
    /// Searches for a memory type index supported by the physical device that matches required property flags.
    /// </summary>
    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var memProperties);
        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                return (uint)i;
        }

        throw new NotSupportedException("Failed to find suitable GPU memory type.");
    }

    /// <summary>
    /// Evaluates candidate image formats and returns the first format supported by the physical device with the requested features.
    /// </summary>
    public Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            _vk.GetPhysicalDeviceFormatProperties(_physicalDevice, format, out var props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
                return format;

            if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
                return format;
        }

        throw new NotSupportedException("Failed to find supported format.");
    }

    /// <summary>
    /// Creates a transient command pool for one-time GPU transfer and synchronization commands.
    /// </summary>
    private void CreateUtilityCommandPool()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.TransientBit,
            QueueFamilyIndex = _graphicsFamilyIndex
        };

        VulkanHelper.Check(_vk.CreateCommandPool(_device, in poolInfo, null, out _utilityCommandPool),
            "Failed to create utility command pool.");
    }

    /// <summary>
    /// Begins recording a one-time command buffer for immediate GPU transfer or barrier commands.
    /// </summary>
    public CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _utilityCommandPool,
            CommandBufferCount = 1
        };

        VulkanHelper.Check(_vk.AllocateCommandBuffers(_device, in allocInfo, out var commandBuffer),
            "Failed to allocate single-time command buffer.");

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        VulkanHelper.Check(_vk.BeginCommandBuffer(commandBuffer, in beginInfo),
            "Failed to begin single-time command buffer.");

        return commandBuffer;
    }

    /// <summary>
    /// Ends recording, submits the single-time command buffer to the graphics queue, and waits for completion.
    /// </summary>
    public void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        VulkanHelper.Check(_vk.EndCommandBuffer(commandBuffer),
            "Failed to end single-time command buffer.");

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        VulkanHelper.Check(_vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, default),
            "Failed to submit single-time command buffer to graphics queue.");

        _vk.QueueWaitIdle(_graphicsQueue);
        _vk.FreeCommandBuffers(_device, _utilityCommandPool, 1, in commandBuffer);
    }

    /// <summary>
    /// Destroys all Vulkan device, surface, and instance resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _vk.DeviceWaitIdle(_device);

        if (_utilityCommandPool.Handle != 0)
        {
            _vk.DestroyCommandPool(_device, _utilityCommandPool, null);
            _utilityCommandPool = default;
        }

        _khrSwapchain.Dispose();
        _vk.DestroyDevice(_device, null);
        _khrSurface.DestroySurface(_instance, _surface, null);
        _khrSurface.Dispose();

        if (_extDebugUtils is ExtDebugUtils debugUtils)
        {
            debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
            debugUtils.Dispose();
        }

        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();

        _disposed = true;
    }
}

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
