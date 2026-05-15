# Textures

Phoenix handles textures through `GLTexture` objects and the asset loading system.

## GLTexture

Represents an OpenGL texture with metadata.

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

### Loading from Binary

```csharp
var texture = BinaryTextureReader.Load(gl, "textures/albedo.bin");
```

Binary textures are produced by the [AssetTool](../asset-pipeline/asset-tool.md) and support BC1/BC3/BC5 compressed formats.

### Loading from Raw Image

```csharp
var texture = new GLTexture(gl, new RenderTextureInfo
{
    Name = "my-texture",
    Width = 512,
    Height = 512,
    Format = GL.Rgba8,
    WrapS = GL.ClampToEdge,
    WrapT = GL.ClampToEdge,
    MinFilter = GL.LinearMipmapLinear,
    MagFilter = GL.Linear,
    Data = imageData  // byte[] pixel data
});
```

### Binding

```csharp
texture.Bind(Silk.NET.OpenGL.Extensions.ImGui.TextureUnit.Texture0);
// or
texture.Bind(Silk.NET.OpenGL.Extensions.ImGui.TextureUnit.Texture1);
// or
texture.Bind(Silk.NET.OpenGL.Extensions.ImGui.TextureUnit.Texture2);
```

In GLSL, associate the slot with a uniform sampler:

```csharp
shader.SetTextureUniform("uAlbedo", texture, slot: 0);
```

```glsl
uniform sampler2D uAlbedo;  // defaults to texture unit 0
```

### Resizing (Render Target Textures)

For dynamic render target textures:

```csharp
texture.Resize(new Silk.NET.Maths.Vector2(1920, 1080));
```

## Textures in UI

The `UI` class can draw textures by name (resolving from `AssetLoader`):

```csharp
// Draw a texture from the asset manifest
UI.DrawImg("textures/my-image", new Vector2(100, 100), new Vector2(256, 256));

// Draw with UV coordinates
UI.DrawImg("textures/my-image",
    new Vector2(100, 100),    // position
    new Vector2(256, 256),    // size
    new Vector2(0, 0),        // UV min
    new Vector2(0.5f, 1f)     // UV max (crop)
);

// Draw from a GLTexture object
UI.DrawImg(texture, new Vector2(100, 100), new Vector2(256, 256));

// Draw from raw texture handle
UI.DrawImg(texture.Handle, new Vector2(512, 512),
    new Vector2(100, 100), new Vector2(256, 256),
    new Vector2(10, 10), new Vector2(200, 200));
```

## Compression Formats

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
