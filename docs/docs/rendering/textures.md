# Textures and bindless

Namespace: `Phoenix.Framework.Rendering.Vulkan`

In Phoenix Framework v2, textures are represented by `VulkanTexture` and managed globally through `BindlessManager`. Instead of binding individual textures to texture units, shaders sample textures from a global descriptor array at Set 1.

---

## The bindless texture model

`Graphics.BindlessManager` allocates a global descriptor array of `sampler2D` images:

```
Set 1, Binding 0: uniform sampler2D uTextures[4096];
```

When a `VulkanTexture` loads:
1. It uploads pixel data to a `VkImage` with optimal tiling.
2. It registers with `BindlessManager`.
3. It receives a unique 32-bit `TextureId` (0 to 4095).
4. When disposed, its ID returns to a free list for reuse.

In your shaders, pass the texture ID through push constants:

```glsl
layout(set = 1, binding = 0) uniform sampler2D uTextures[];

void main() {
    vec4 albedo = texture(uTextures[nonuniformEXT(uTextureId)], vTexCoords);
}
```

This model eliminates per-texture descriptor set allocation and descriptor rebinding during scene rendering.

---

## Loading textures

Load textures through `AssetLoader`:

```csharp
VulkanTexture diffuse = AssetLoader.LoadTexture("Textures/brick_diffuse.png");
uint textureId = diffuse.TextureId;
```

`AssetLoader` automatically checks `.contentbin/` for pre-compressed `.dds` binaries. In production, pre-compressed files upload directly to the GPU without CPU transcoding.

---

## Supported compression formats

Block-compressed formats reduce GPU memory usage and bandwidth:

| Format | Vulkan format | Bytes/pixel | Best for |
|---|---|---|---|
| **RGBA8** | `R8G8B8A8_UNORM` | 4.0 | UI icons, dynamic canvas textures, text |
| **BC1 (DXT1)** | `BC1_RGB_UNORM_BLOCK` | 0.5 | Opaque albedo and diffuse textures |
| **BC3 (DXT5)** | `BC3_UNORM_BLOCK` | 1.0 | Albedo textures with alpha masks |
| **BC5 (RGTC2)** | `BC5_UNORM_BLOCK` | 1.0 | Tangent-space normal maps and height maps |
| **BC7 (BPTC)** | `BC7_UNORM_BLOCK` | 1.0 | High-quality PBR textures, roughness, and metalness |

---

## GPU mipmap generation

For uncompressed textures without precomputed mipmaps, `VulkanTexture` can generate a complete mip chain on the GPU using blit operations and `VK_KHR_synchronization2` pipeline barriers:

```csharp
var texture = new VulkanTexture(Graphics.Context, imageWidth, imageHeight, format);
texture.GenerateMipmaps();
```

---

## Sampler caching (SamplerManager)

`SamplerManager` caches immutable `VkSampler` handles for address modes and filters:

| Filter mode | Address mode | Description |
|---|---|---|
| Linear | Repeat | Bilinear filtering with wrapping texture coordinates |
| Linear | ClampToEdge | Bilinear filtering clamped to edge pixels (skyboxes, UI) |
| Nearest | ClampToEdge | Point sampling for pixel art or G-buffer lookups |
| Linear | MirroredRepeat | Bilinear filtering mirrored on border wrap |

Each sampler can enable 16x anisotropic filtering to reduce blur on angled surfaces.
