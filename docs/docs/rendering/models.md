# Models & Animation

Phoenix loads 3D models from binary format (compiled from Assimp by [AssetTool](../asset-pipeline/asset-tool.md)). Supports static meshes and skeletal animation.

## Loading Models

```csharp
// Load from asset manifest
var model = AssetLoader.LoadModel("characters/robot");

// Load as static model
var staticModel = (Model)AssetLoader.LoadModel("props/box");

// Load as animated model
var animatedModel = (AnimatedModel)AssetLoader.LoadModel("characters/walk");
```

## Model Hierarchy

```
Model
 ├── Parts: List<ModelPart>
 │     └── Name: string
 │     └── Meshes: List<ModelMesh>
 │           └── Name: string
 │           └── Transform: Matrix4x4
 │           └── Draw()
 └── TextureNames: List<string>

AnimatedModel : Model
 ├── Animations: List<Animation>
 ├── AnimatorNodes: AnimatorNode[]
 ├── InverseGlobalTransform: Matrix4x4[]
 ├── BoneCount: int
 ├── FinalBoneMatrices: Matrix4x4[]
 ├── SetAnimation(int index)
 └── Update(float deltaTime)
```

## Rendering a Static Model

```csharp
var model = (Model)AssetLoader.LoadModel("props/tree");

foreach (var part in model.Parts)
{
    foreach (var mesh in part.Meshes)
    {
        // Set your shader uniforms (model matrix, textures, etc.)
        shader.SetUniform("uModelMatrix", mesh.Transform);
        shader.SetUniform("uAlbedo", albedoTexture, slot: 0);
        shader.SetAsCurrentGLProgram();

        // Draw the mesh
        mesh.Draw();
    }
}
```

## Rendering an Animated Model

### Setup

```csharp
var model = (AnimatedModel)AssetLoader.LoadModel("characters/robot");
model.SetAnimation(0);  // Select animation by index
```

### In Update Loop

```csharp
protected override void Update(double dt)
{
    model.Update((float)dt);  // Advance animation, compute bone transforms
}

protected override void Render(double dt)
{
    foreach (var part in model.Parts)
    {
        foreach (var mesh in part.Meshes)
        {
            // Pass bone matrices to shader
            shader.SetUniform("boneMatrices", model.FinalBoneMatrices);
            shader.SetAsCurrentGLProgram();
            mesh.Draw();
        }
    }
}
```

## AnimatedModel Properties

| Property | Type | Description |
|----------|------|-------------|
| `Animations` | `List<Animation>` | All animations in the model |
| `AnimatorNodes` | `AnimatorNode[]` | Bone hierarchy nodes |
| `InverseGlobalTransform` | `Matrix4x4[]` | Bone offset matrices |
| `BoneCount` | `int` | Number of bones |
| `FinalBoneMatrices` | `Matrix4x4[]` | Final world-space bone matrices (updated by `Update`) |

## Animations

Each `Animation` represents one animation clip (e.g., "Idle", "Walk", "Attack").

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Animation name |
| `Duration` | `float` | Total duration in seconds |
| `TicksPerSecond` | `float` | Keyframe ticks per second |
| `CurrentFrame` | `Transform[]` | Interpolated transforms per bone |
| `Transforms` | `Matrix4x4[]` | Final matrix per bone |

### Playing Animations

```csharp
model.SetAnimation(0);  // First animation (0-indexed)
model.SetAnimation(1);  // Second animation

// Access animation properties
var anim = model.Animations[0];
Console.WriteLine($"Duration: {anim.Duration}s, Name: {anim.Name}");
```

### Keyframe Structure

Each animation contains per-bone keyframe arrays:

```
Animation
 └── BoneKeyframes: Keyframe[][]
       └── Keyframe
            ├── TimeStamp: float
            └── Transform: Transform
                 ├── Scale: Vector3
                 ├── Rotation: Quaternion
                 └── Translation: Vector3
```

## ModelMesh

Each mesh within a model part. Contains vertex data (VAO) and index data.

### Vertex Layout

| Attribute | Type | Location |
|-----------|------|----------|
| `Position` | `Vector3` | 0 |
| `Normal` | `Vector3` | 1 |
| `TexCoord` | `Vector2` | 2 |
| `Tangent` | `Vector3` | 3 |
| `Bitangent` | `Vector3` | 4 |
| `BoneIds` | `Vector4<int>` | 5 |
| `Weights` | `Vector4<float>` | 6 |

### Drawing

```csharp
mesh.Draw();  // GL.BindVertexArray + GL.DrawElements
```

## Bone Animation Details

Bone transforms are computed in `AnimatedModel.Update(deltaTime)`:

1. **Interpolate** keyframes for each bone based on elapsed time
2. **Apply** local bone transforms (scale, rotation, translation)
3. **Propagate** transforms down the bone hierarchy (parent × child)
4. **Combine** with `InverseGlobalTransform` offsets for final world-space matrices
5. Store results in `FinalBoneMatrices[]` for the shader

The shader applies skinning using the 4 bone IDs and weights per vertex:

```glsl
// In vertex shader:
for (int i = 0; i < 4; i++) {
    skinPos += boneMatrices[int(boneIds[i])][i] * boneWeights[i];
}
```

## Binary Model Format

Models are stored as `.bin` files compiled by [AssetTool](../asset-pipeline/asset-tool.md):

```
Header: "Model" + version + isAnimated + hasTangents
├── Parts[]
│     └── Name, Meshes[]
│           └── Name, Indices[], Transform, Vertices[]
└── (if animated)
     ├── InverseGlobalTransform[]
     ├── AnimatorNodes[]
     └── Animations[]
           └── Name, Duration, TicksPerSecond
                 └── BoneKeyframes[][]
```

## See Also

- [AssetLoader](../asset-pipeline/loading.md) — loading models by name
- [AssetTool](../asset-pipeline/asset-tool.md) — compiling models to binary format
- [Shaders](shaders.md) — passing bone matrices to shaders
