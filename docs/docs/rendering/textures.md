# Textures

Phoenix handles textures through `GLTexture` objects and are loaded the [Asset loading](../asset-pipeline/loading.md) system.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Texture identifier |
| `Handle` | `uint` | OpenGL texture handle |
| `Size` | `Vector2` | Base texture dimensions |
| `WrapS` / `WrapT` | `int` | Wrap mode (S and T axes) |
| `FilterMin` / `FilterMag` | `int` | Minification and magnification filters |
| `Format` | `byte` | Compression format: `0`=RGBA8, `1`=BC1, `2`=BC3, `3`=BC5 |
| `MipCount` | `int` | Number of mipmaps |
| `MipSizes` | `Vector2[]` | Size of each mipmap level |

## Supported compression Formats

| Format | Internal | Bytes/pixel | Quality | Best For |
|--------|----------|-------------|---------|----------|
| RGBA8 | `GL.Rgba8` | 4 | Lossless | Small textures, UI |
| BC1 (DXT1) | `GL.CompressedSrgbAlphaTextureS3TCDXT1` | 0.5 | Lossy | Albedo maps |
| BC3 (DXT5) | `GL.CompressedSrgbAlphaTextureS3TCBC3` | 1 | Lossy | RGBA textures with alpha |
| BC5 (RGTC2) | `GL.CompressedSignedRedGreenTextureS3TCBC5` | 1 | Lossless | Normal maps, height maps |

## See Also

- [AssetLoader](../asset-pipeline/loading.md) — resolves textures by name from manifest
- [Render Targets](../core/graphics.md#render-targets) — creates dynamic textures
- [UI & ImGui](ui.md) — draws textures in overlay
