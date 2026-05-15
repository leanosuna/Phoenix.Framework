# Shaders

`GLShader` compiles and manages OpenGL shader programs (vertex + fragment).

## Creating a Shader

### From Path (auto-finds .vert + .frag)

```csharp
var shader = new GLShader(gl, "shaders/pbr");
// Looks for: "shaders/pbr.vert" and "shaders/pbr.frag"
```

### Explicit Stages

```csharp
var shader = new GLShader(gl,
    "shaders/post-process.vert",
    "shaders/post-process.frag"
);
```

### Ignore Missing Uniforms

If some uniforms may not exist (e.g., optional features), suppress errors:

```csharp
var shader = new GLShader(gl, "shaders/pbr", ignoreUniformsNotFound: true);
```

## Activating

```csharp
shader.SetAsCurrentGLProgram();
// ... draw calls ...
shader.SetAsCurrentGLProgram();  // No-op if already active
```

Check if it is currently active:

```csharp
bool isActive = shader.IsCurrent();
```

## Uniforms

### Setting a Uniform by Location

```csharp
int loc = shader.GetUniformLocation("uDiffuseMap");
shader.SetUniform(loc, someTextureHandle);
```

### Setting a Uniform by Name

```csharp
shader.SetUniform("uView", cameraViewMatrix);
shader.SetUniform("uProjection", cameraProjectionMatrix);
shader.SetUniform("uTime", (float)Graphics.Time);
shader.SetUniform("uOpacity", 0.75f);
shader.SetUniform("uColor", new Vector4(1f, 0f, 0f, 1f));
shader.SetUniform("uPosition", new Vector3(0, 1, 0));
shader.SetUniform("uScale", new Vector2(2f, 2f));
shader.SetUniform("uModelMatrix", modelMatrix);
```

### Arrays

```csharp
// Array of vectors
shader.SetUniform("uBoneMatrices", boneMatrices);  // Matrix4x4[]

// Array of floats
shader.SetUniform("uWeights", weights);  // float[]

// Array of vectors
shader.SetUniform("uColors", colors);  // Vector4[]
```

### Texture Uniforms

```csharp
// By texture handle and slot
shader.SetTextureUniform("uAlbedo", textureHandle, slot: 0);

// By GLTexture object
shader.SetTextureUniform("uNormal", albedoTexture, slot: 0);

// In GLSL:
// uniform sampler2D uAlbedo;
// vec4 color = texture(uAlbedo, uv);
```

## UBO (Uniform Buffer Object)

Attach the CommonUBO or a custom UBO for bulk uniform data:

```csharp
shader.AttachUBO(commonUboHandle, "CommonData", binding: 0);
```

### CommonUBO GLSL Layout

```glsl
layout(std140) uniform CommonData {
    mat4 sView;           // layout(location = 0)
    mat4 sProjection;     // layout(location = 1)
    float sTime;          // layout(location = 2)
    float sDeltaTime;     // layout(location = 3)
};
```

This is updated automatically every frame by `PhoenixGame` with the current camera view/projection and timing.

## Error Handling

If a uniform name is not found in the shader, an error is added to [`ErrorListWindow`](../utilities/logging.md):

```
Could not find uniform: "uSomeUniform" in shader "shaders/pbr"
```

Set `ignoreUniformsNotFound: true` on construction to suppress these.

## Disposal

```csharp
shader.Dispose();  // Deletes GL program handle
```

## GLSL Shader Templates

### Standard PBR Vertex Shader

```glsl
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in vec4 aBoneIds;
layout(location = 4) in vec4 aBoneWeights;

uniform mat4 sView;
uniform mat4 sProjection;
uniform mat4 boneMatrices[20];

out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vWorldPos;

void main()
{
    vec4 skinPos = vec4(aPosition, 1.0);
    for (int i = 0; i < 4; i++)
    {
        skinPos += boneMatrices[int(aBoneIds[i] * 255)][i] * aBoneWeights[i] * skinPos;
    }
    gl_Position = sProjection * sView * skinPos;
    vNormal = aNormal;
    vTexCoord = aTexCoord;
    vWorldPos = skinPos.xyz;
}
```

### Standard PBR Fragment Shader

```glsl
#version 330 core
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vWorldPos;

uniform sampler2D uAlbedo;
uniform sampler2D uNormal;
uniform sampler2D uMetallicRoughness;
uniform vec3 uCameraPos;

out vec4 fragColor;

void main()
{
    vec3 albedo = texture(uAlbedo, vTexCoord).rgb;
    vec3 normal = texture(uNormal, vTexCoord).rgb * 2.0 - 1.0;
    float metallic = texture(uMetallicRoughness, vTexCoord).r;
    float roughness = texture(uMetallicRoughness, vTexCoord).g;

    vec3 N = normalize(normal);
    vec3 V = normalize(uCameraPos - vWorldPos);

    fragColor = vec4(albedo, 1.0);
}
```

## See Also

- [CommonUBO](../core/graphics.md#commonubo) — shared uniform buffer
- [Models & Animation](models.md) — models pass bone matrices to shaders
- [Gizmos](gizmos.md) — gizmos use their own internal shader
