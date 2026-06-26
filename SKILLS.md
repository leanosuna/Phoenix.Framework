# Phoenix Framework — Skills & API Reference

**Docs:** https://framework.nx.net.ar  
**Source:** https://github.com/leanosuna/Phoenix.Framework  
**NuGet:** `Phoenix.Framework 1.0.1`  
**Asset Tool:** `Phoenix.AssetTool` (`dotnet pat`)

---

## Table of Contents

- [Getting Started](#getting-started)
- [Core Systems](#core-systems)
  - [Game Loop](#game-loop)
  - [Graphics & Rendering](#graphics--rendering)
  - [Camera](#camera)
  - [Input](#input)
- [Rendering](#rendering)
  - [Shaders](#shaders)
  - [Textures](#textures)
  - [Models & Animation](#models--animation)
  - [UI & ImGui](#ui--imgui)
  - [Gizmos](#gizmos)
- [Collision System](#collision-system)
- [Audio](#audio)
- [Asset Pipeline](#asset-pipeline)
- [Utilities](#utilities)

---

## Getting Started

```bash
dotnet new sln -n MyGame
dotnet new console -n MyGame -o MyGame
dotnet sln add MyGame/MyGame.csproj
cd MyGame
dotnet add package Phoenix.Framework
dotnet tool install Phoenix.AssetTool
mkdir Content
dotnet pat Content/asset-manifest.json init
```

### csproj setup

```xml
<ItemGroup>
  <None Include="Content\ContentBin\**" CopyToOutputDirectory="PreserveNewest" />
  <None Include="Content\asset-manifest.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
<Target Name="Phoenix-AssetTool-Build" BeforeTargets="BeforeBuild">
  <Exec Command="dotnet pat Content/asset-manifest.json build" StandardOutputImportance="high" />
</Target>
```

### Entry point

```csharp
public static class Program
{
    public static void Main()
    {
        var game = new Game();
        game.Run();
    }
}
```

---

## Core Systems

### Game Loop

`PhoenixGame` is the abstract base class. Internal pipeline:

```
Run()
 └── Window.Create()
      └── Window.Load       → InternalLoad()
           ├── InputManager created
           ├── UI created (ImGui initialized)
           ├── RTManager created
           ├── Scene render target created
           ├── Graphics created
           └── CommonUBO created
      └── Window.Update     → InternalUpdate()
           ├── Frame 0: DelayedLoad()
           │    ├── FullScreenQuad created
           │    ├── Gizmos created
           │    ├── SoundManager initialized
           │    ├── AssetLoader initialized
           │    └── Initialize() called
           ├── Graphics.Time += dt
           ├── InputManager.Update()
           ├── Render halt check (F11)
           ├── Update(dt)          ← user code
           ├── CommonUBO updated
           └── Gizmos.Update() if enabled
      └── Window.Render     → InternalRender()
           ├── UI.Update(dt)
           ├── If not halted:
           │    ├── SetRenderToTarget(_sceneRT)
           │    ├── Render(dt)        ← user code
           │    └── Gizmos.Render() if enabled
           └── TrueRenderToScreen()
               ├── ClearRenderTarget()
               ├── UI.Render()     ← ImGui overlay
               └── RenderUI(dt)    ← user overlay code
```

**Methods to override:**

| Method | Required | Description |
|---|---|---|
| `Initialize()` | Yes | Load assets, configure systems |
| `Update(double dt)` | Yes | Per-frame game logic |
| `Render(double dt)` | Yes | Per-frame rendering |
| `InitialLoadScreen()` | No | Custom loading screen before `Initialize()` |
| `OnWindowResize(Vector2 size)` | No | Handle resize |
| `OnClose()` | No | Custom cleanup |

**Properties:**

| Property | Type | Notes |
|---|---|---|
| `GL` | `GL` | Silk.NET OpenGL context |
| `Window` | `IWindow` | Silk.NET window handle |
| `WindowSize` | `Vector2` | Current window dimensions |
| `WindowWidth` | `int` | Width shortcut |
| `WindowHeight` | `int` | Height shortcut |
| `InputManager` | `InputManager` | Read-only |
| `FullScreenQuad` | `FullScreenQuad` | Single quad for post-processing |
| `Gizmos` | `Gizmos` | Debug drawing |
| `UI` | `UI` | ImGui and text overlay |
| `Camera` | `Camera` | Settable — assign in `Initialize()` |
| `Graphics` | `Graphics` | Read-only after load |
| `CommonUboHandle` | `uint` | UBO bound at binding point 0 |

The **CommonUBO** is updated every frame with `[View, Projection, Time, DeltaTime]` at binding point 0.

**Render halt:** F11 toggles, stops Update/Render but keeps UI alive.

---

### Graphics & Rendering

`Graphics` manages OpenGL state, render targets, and the rendering pipeline. All methods are idempotent and track previous state.

**GL State Management:**

```csharp
Graphics.SetDepthTest(true);
Graphics.SetDepthTest(true, GLEnum.Less);
Graphics.SetAlphaBlend(true);
Graphics.SetAlphaBlend(true, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
Graphics.SetFaceCulling(true);
Graphics.SetFaceCulling(true, GLEnum.Back, frontIsCcw: true);
Graphics.SetClearColor(new Vector4(0.1f, 0.15f, 0.2f, 1f));
Graphics.ClearRenderTarget();
Graphics.ClearRenderTarget(true, true, false); // color + depth
```

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Time` | `double` | Total elapsed time |
| `FPS` | `double` | Instantaneous FPS |
| `FPS_SAMPLE` | `double` | Averaged FPS (default 0.3s sample) |
| `FT_SAMPLE` | `double` | Frame time sample |
| `RenderHaltKey` | `Key` | Key to toggle render halt (default `Key.F11`) |
| `RenderViewport` | — | `RenderViewport.Scale` controls resolution scaling |

**Render Targets (fluent builder):**

```csharp
// Simple
var rt = Graphics.NewRenderTarget();
var rt = Graphics.BuildRenderTarget().AddTexture().AddDepthBuffer().Build();

// Custom
var rt = Graphics.BuildRenderTarget()
    .SetName("post-process-rt")
    .AddTexture(RTManager.BuildRTT()
        .SetFormat(GLEnum.Rgba8)
        .SetWrapS(GLEnum.ClampToEdge)
        .SetMinFilter(GLEnum.Linear)
        .SetStatic(new Vector2(1024, 1024)))
    .SetDepthBuffer(new DepthBuffer())
    .Build();

// Dynamic resolution (follows window)
var rt = Graphics.BuildRenderTarget()
    .SetName("half-res-rt")
    .AddTexture(RTManager.BuildRTT()
        .SetResolutionMultiplier(new Vector2(0.5f, 0.5f)))
    .AddDepthBuffer()
    .Build();
```

**Depth Buffer:** `new DepthBuffer()` (window-sized, Depth24Stencil8), `new DepthBuffer(GLEnum.Depth24Stencil8)`, `new DepthBuffer(new Vector2(1024, 1024))`

**Usage:**

```csharp
Graphics.SetRenderToTarget(rt);
// ... draw to rt ...
Graphics.SetRenderToScreen();
```

**Finding targets:** `Graphics.TryFindByName("name", out var rt)`, `Graphics.FindByName("name")` (throws if not found)

**FullScreenQuad:** Single quad from -1 to 1 with UV 0→1. `Graphics.FullScreenQuad.Draw();`

**CommonUBO (GLSL side):**
```glsl
layout(std140) uniform CommonData {
    mat4 sView;
    mat4 sProjection;
    float sTime;
    float sDeltaTime;
};
```

**C# binding:** `shader.AttachUBO(Game.CommonUboHandle, "CommonData", binding: 0);`

**RenderViewport:** `Graphics.RenderViewport.Scale = new Vector2(0.5f, 0.5f);` — half resolution rendering.

---

### Camera

**Hierarchy:**
```
Camera (abstract)
 └── BaseCamera (abstract)
      └── MouseCamera (abstract)
           └── FreeCamera (concrete)
```

**FreeCamera:**

```csharp
Camera = new FreeCamera(
    this,                           // PhoenixGame reference
    new Vector3(0, 5, -10),         // Position
    0,                              // Yaw (radians)
    -MathF.PI / 4,                  // Pitch
    MathF.PI / 4,                   // Max pitch
    0.1f,                           // Near plane
    1000f,                          // Far plane
    WindowWidth / (float)WindowHeight
);
```

**Movement keys:**

```csharp
camera.SetMoveKeys(Key.W, Key.S, Key.A, Key.D, Key.E, Key.Q, Key.LeftShift, moveSpeedMultiplier: 2f);
camera.MoveSpeed = 15f;
```

**Mouse look:** `camera.MouseAim = true;` (default). Sensitivity: `camera.MouseSensitivity = 0.002f;` (default 0.001f)

**Manual yaw/pitch keys:**

```csharp
camera.SetPitchYawKeys(Key.Up, Key.Down, Key.Left, Key.Right, new Vector2(2f, 2f));
```

**Update:** Call `Camera.Update((float)dt);` each frame in `Update()`.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Position` | `Vector3` | Camera position |
| `Front` | `Vector3` | Forward direction (normalized) |
| `Up` | `Vector3` | Up vector |
| `Right` | `Vector3` | Right vector |
| `Yaw` | `float` | Horizontal rotation (radians) |
| `Pitch` | `float` | Vertical rotation (radians) |
| `FOV` | `float` | Field of view (radians) |
| `AspectRatio` | `float` | Width / Height |
| `NearPlane` | `float` | Near clipping plane |
| `FarPlane` | `float` | Far clipping plane |
| `View` | `Matrix4x4` | View matrix |
| `Projection` | `Matrix4x4` | Projection matrix |
| `MoveSpeed` | `float` | Movement speed (default 10) |
| `MouseAim` | `bool` | Process mouse delta (default true) |
| `MouseSensitivity` | `float` | Mouse sensitivity (default 0.001) |

Camera matrices are automatically fed into the CommonUBO every frame.

**Custom camera:** Inherit from `BaseCamera` and override `Update(float deltaTime)`.

---

### Input

`InputManager` is accessible via `this.InputManager` on `PhoenixGame`.

**Keyboard:**

```csharp
// Held this frame
if (InputManager.KeyDown(Key.W)) { }

// Edge-detect (first frame only)
if (InputManager.KeyDownOnce(Key.E)) { }
```

**Mouse:**

```csharp
Vector2 delta = InputManager.MouseDelta;
float wheel = InputManager.MouseWheelValue;

InputManager.SetMouseMode(CursorMode.Raw);    // Lock cursor
InputManager.SetMouseMode(CursorMode.Normal); // Unlock
InputManager.ToggleMouseMode();
InputManager.MouseSensitivity = 0.002f; // Default 0.001f
```

---

## Rendering

### Shaders

The AssetTool generates `partial ShaderHelper` child classes for each shader pair.

**ShaderHelper API:**

```csharp
public void Use()                                          // Set as active program
public void AttachUBO(uint bufferHandle, string name, uint binding = 0)
public void Dispose()
```

**Generated class example:**

```csharp
public partial class ShaderBasicModel : ShaderHelper
{
    public ShaderUniform<Vector3> CameraPosition { get; private set; }
    public ShaderUniform<float> KA { get; private set; }
    public ShaderUniform<float> KD { get; private set; }
    public ShaderUniform<float> KS { get; private set; }
    public ShaderUniform<Vector3> LightColor { get; private set; }
    public ShaderUniform<Vector3> LightPosition { get; private set; }
    public ShaderTextureUniform TexColor { get; private set; }
    public ShaderUniform<Matrix4x4> World { get; private set; }

    public ShaderBasicModel()
    {
        _shader = AssetLoader.LoadShader("Shaders/basicModel/basicModel");
        CameraPosition = new ShaderUniform<Vector3>(_shader, "CameraPosition");
        // ...
    }
}
```

Uniforms optimized out by the GLSL compiler are not generated, preventing runtime errors.

---

### Textures

Handled via `GLTexture` objects loaded through the AssetLoader.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Texture identifier |
| `Handle` | `uint` | OpenGL texture handle |
| `Size` | `Vector2` | Base texture dimensions |
| `WrapS` / `WrapT` | `int` | Wrap modes |
| `FilterMin` / `FilterMag` | `int` | Filters |
| `Format` | `byte` | 0=RGBA8, 1=BC1, 2=BC3, 3=BC5 |
| `MipCount` | `int` | Number of mipmaps |
| `MipSizes` | `Vector2[]` | Size per mip level |

**Supported compression:**

| Format | Internal | Bytes/px | Quality | Use |
|---|---|---|---|---|
| RGBA8 | `GL.Rgba8` | 4 | Lossless | UI, small textures |
| BC1 (DXT1) | `GL.CompressedSrgbAlphaTextureS3TCDXT1` | 0.5 | Lossy | Albedo maps |
| BC3 (DXT5) | `GL.CompressedSrgbAlphaTextureS3TCBC3` | 1 | Lossy | RGBA with alpha |
| BC5 (RGTC2) | `GL.CompressedSignedRedGreenTextureS3TCBC5` | 1 | Lossless | Normal maps |

---

### Models & Animation

**Loading:**

```csharp
var staticModel = (Model)AssetLoader.LoadModel("props/box");
var animatedModel = (AnimatedModel)AssetLoader.LoadModel("characters/walk");
```

**Model hierarchy:**
```
Model
 ├── Parts: List<ModelPart>
 │     └── Name: string
 │     └── Meshes: List<ModelMesh>
 │           └── Name, Transform, Draw()
 └── TextureNames: List<string>

AnimatedModel : Model
 ├── Animations, AnimatorNodes, InverseGlobalTransform
 ├── BoneCount, FinalBoneMatrices
 ├── SetAnimation(int index)
 └── Update(float deltaTime)
```

**Rendering static:**

```csharp
foreach (var part in model.Parts)
    foreach (var mesh in part.Meshes)
    {
        ShaderBasicModel.Use();
        ShaderBasicModel.World.Set(mesh.Transform);
        ShaderBasicModel.Albedo.Set(albedoTexture);
        mesh.Draw();
    }
```

**Rendering animated:**

```csharp
model.SetAnimation(0);
// In Update:
model.Update((float)dt);
// In Render:
foreach (var part in model.Parts)
    foreach (var mesh in part.Meshes)
    {
        ShaderAnimated.Use();
        ShaderAnimated.BoneMatrices.Set(model.FinalBoneMatrices);
        mesh.Draw();
    }
```

**Animation properties:**

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Animation clip name |
| `Duration` | `float` | Duration in seconds |
| `TicksPerSecond` | `float` | Keyframe ticks/sec |
| `CurrentFrame` | `Transform[]` | Interpolated bone transforms |
| `Transforms` | `Matrix4x4[]` | Final bone matrices |

**Vertex layout (location → attribute):**

| Location | Attribute | Type |
|---|---|---|
| 0 | Position | `Vector3` |
| 1 | Normal | `Vector3` |
| 2 | TexCoord | `Vector2` |
| 3 | Tangent | `Vector3` |
| 4 | Bitangent | `Vector3` |
| 5 | BoneIds | `Vector4<int>` |
| 6 | Weights | `Vector4<float>` |

---

### UI & ImGui

`UI` is automatically created by `PhoenixGame`.

**Drawing text:**

```csharp
UI.DrawText("Hello", new Vector2(100, 100), new Vector4(1, 1, 1, 1), size: 24);
UI.DrawCenteredText("Score", new Vector2(200, 50), new Vector4(1, 1, 1, 1), 32);
UI.DrawHCenteredText("Centered", new Vector2(400, 50), new Vector4(1, 1, 1, 1), 20);
UI.DrawRAlignedText("FPS: 60", new Vector2(800, 10), new Vector4(1, 1, 1, 1), 16);
```

**Drawing images:**

```csharp
UI.DrawImg("textures/ui-panel", new Vector2(0, 0), new Vector2(800, 600));
UI.DrawImg("textures/sprite-sheet", pos, size, uvMin, uvMax);
UI.DrawImg(myTexture, pos, size);
UI.DrawImg(myTexture.Handle, srcPos, srcSize, dstPos, dstSize);
```

**Simple buttons:** `UI.DrawSimpleButton("Click Me", pos, size, () => { ... });`

**Custom ImGui** (inside `RenderUI()` override):
```csharp
protected override void RenderUI()
{
    ImGui.Begin("Debug Panel");
    ImGui.Text($"FPS: {Graphics.FPS_SAMPLE:F0}");
    ImGui.End();
}
```

**Font management:** Cascadia Mono loaded by default at sizes 10-100 (1px steps).
```csharp
UI.LoadFontTTF("fonts/myfont.ttf", new int[] { 16, 24, 32, 48 });
UI.SetFontSize(24);
```

**ErrorListWindow** — centralized error overlay:
```csharp
ErrorListWindow.Add("Something went wrong");
ErrorListWindow.Add("Temporary", showTimeSeconds: 5f);
ErrorListWindow.Show = true;
```
Errors are deduplicated (count displayed). Hover shows caller info.

---

### Gizmos

Debug wireframe drawing. `Gizmos.Enabled = true;` (default).

**Lines:**

```csharp
Gizmos.AddLine(Vector3.Zero, new Vector3(0, 5, 0), new Vector3(1, 1, 1));
Gizmos.AddLine(Vector3.Zero, new Vector3(10, 0, 0), new Vector3(1, 0, 0), hit: true);
```

**Cubes:**

```csharp
Gizmos.AddCube(Vector3.Zero, new Vector3(2, 2, 2), new Vector3(0, 1, 0));
Gizmos.AddCube(worldMatrix, new Vector3(1, 1, 0));
```

**Spheres:**

```csharp
Gizmos.AddSphere(new Vector3(0, 1, 0), 2.5f, new Vector3(0, 0, 1));
Gizmos.AddSphere(enemyTransform, new Vector3(1, 1, 0));
```

**Cylinders, Planes, Axis lines:**

```csharp
Gizmos.AddCylinder(pos, radius, height, rotation, color);
Gizmos.AddPlane(pos, normal, size, color);
Gizmos.AddAxisLines(5);
```

**Collision volumes:**

```csharp
Gizmos.AddVolume(sphere, new Vector3(1, 0, 0));
Gizmos.AddVolume(aabb, new Vector3(0, 1, 0));
Gizmos.AddVolume(obb, new Vector3(0, 0, 1));
Gizmos.AddVolume(frustum, new Vector3(1, 1, 0));
Gizmos.AddVolume(cylinder, new Vector3(1, 0.5f, 0.5f));
```

**Colors:** RGB 0-1: `Vector3(1,0,0)` = Red, `(0,1,0)` = Green, `(0,0,1)` = Blue, etc.  
The `hit: true` parameter inverts colors for collision visualization.

| Geometry | Internal Class | Vertices | Edges |
|---|---|---|---|
| Line | `GGLineSegment` | 2 | 1 |
| Cube | `GGCube` | 8 | 12 |
| Sphere | `GGSphere` | 64×3 circles | 192 |
| Cylinder | `GGCylinder` | 64×3+4 | 196 |
| Plane | `GGPlane` | 4 | 5 |

---

## Collision System

All pure math — no physics engine.

**Volumes:**

| Volume | Description |
|---|---|
| `Ray` | Origin + direction |
| `BoundingSphere` | Center + radius |
| `AxisAlignedBoundingBox` | Min/Max bounds |
| `OrientedBoundingBox` | Position + size + orientation |
| `BoundingFrustum` | 6-plane view frustum |
| `BoundingCylinder` | Position + radius + height + rotation |

**Intersection tests:**

```csharp
// Ray
float? hitDist = ray.Intersects(sphere);
float? hitDist = ray.Intersects(aabb);
float? hitDist = ray.Intersects(plane);
Vector3? triHit = ray.Intersects(v0, v1, v2); // Möller-Trumbore

// Sphere
bool hits = sphere.Intersects(aabb);
ContainmentType type = sphere.Contains(point);

// AABB
bool hits = aabb.Intersects(frustum);
Vector3[] corners = aabb.GetCorners();

// Frustum
bool inFrustum = frustum.Contains(point);
frustum.Update(Camera.View * Camera.Projection);

// OBB
bool hits = obb.Intersects(ray, out float? dist);

// Cylinder
bool hits = cylinder.Intersects(sphere);
```

**Creating volumes:**

```csharp
var sphere = BoundingSphere.CreateFromPoints(vertices);
var aabb = AxisAlignedBoundingBox.CreateFromPoints(vertices);
var sphereFromBox = BoundingSphere.CreateFromBoundingBox(aabb);
var merged = BoundingSphere.CreateMerged(sphereA, sphereB);
var obb = OrientedBoundingBox.FromAABB(aabb);
```

**Transforms:**

```csharp
var transformed = sphere.Transform(worldMatrix);
var transformedBox = aabb.Transform(worldMatrix);
```

**OBB space conversions:**

```csharp
Vector3 local = obb.ToOBBSpace(worldPoint);
Vector3 world = obb.ToWorldSpace(localPoint);
```

**PlaneHelper:**

```csharp
PlaneIntersectionType type = PlaneHelper.ClassifyPoint(plane, point);
float dist = PlaneHelper.PerpendicularDistance(plane, point);
Plane t = PlaneHelper.Transform(plane, matrix);
Plane t = PlaneHelper.Transform(plane, rotation);
```

---

## Audio

OpenAL-based with pluggable decoders. `SoundManager` is initialized automatically by `PhoenixGame`.

**Loading and playing:**

```csharp
var clip = SoundManager.LoadSound("sounds/explosion.wav");
var instance = SoundManager.Play2D(clip, volume: 0.8f);
var instance = SoundManager.Play3D(clip, new Vector3(5, 1, 0), volume: 0.8f);
```

**Playback control:**

```csharp
instance.Play(); instance.Pause(); instance.Stop();
instance.SetVolume(0.5f); instance.SetPitch(1.5f);
instance.SetPosition(new Vector3(1, 2, 3));
instance.SetVelocity(Vector3.Zero);
bool playing = instance.IsPlaying;
instance.Dispose();
```

**Listener configuration:**

```csharp
SoundManager.SetListenerPosition(Camera.Position);
SoundManager.SetListenerVelocity(velocity);
SoundManager.SetListenerOrientation(Camera.Front, Camera.Up);
```

`SoundManager.Update()` is called automatically each frame, cleaning up finished instances.

**Custom decoder:**

```csharp
public class OggDecoder : IAudioDecoder
{
    public bool CanDecode(string path) => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
    public SoundData Decode(string path) { /* decode to PCM */ }
}
SoundManager.RegisterDecoder(new OggDecoder());
```

**SoundData:** `byte[] PCM`, `BufferFormat Format` (Mono8/16, Stereo8/16), `int SampleRate`

**Built-in WAV decoder:** 8/16-bit PCM, mono/stereo, any sample rate. No compressed formats.

---

## Asset Pipeline

### AssetLoader

Static class with caching:

```csharp
Model model = AssetLoader.LoadModel("characters/robot");
GLTexture texture = AssetLoader.LoadTexture("textures/brick-albedo");
// Shaders loaded via generated ShaderHelper classes
```

All loaded assets are cached internally (subsequent calls return same instance).

### AssetManifest

JSON file mapping asset names to paths. Initialized with `dotnet pat manifest init`.

```json
{
    "Assets": [
        { "RelativePath": "Models/characters/robot.bin", "Type": 0 },
        { "RelativePath": "Textures/brick-albedo", "Type": 1 },
        { "RelativePath": "Shaders/animated/animated.frag", "Type": 3 },
        { "RelativePath": "Shaders/animated/animated.vert", "Type": 3 }
    ],
    "Namespace": "Phoenix.Framework.ShaderHelpers",
    "DarkTheme": true
}
```

**AssetType:**

| Value | Name | Description |
|---|---|---|
| 0 | Model | Binary model (`.bin`) |
| 1 | Texture | Binary texture |
| 2 | ExtTexture | Texture extracted from a model |
| 3 | Shader | `.vert` / `.frag` |
| 4 | Unknown | — |

**Content folder structure:**

```
Content/                  # Source assets
 ├── characters/robot.fbx
 └── textures/brick-albedo.png
ContentBin/               # Compiled binary assets
 ├── characters/robot.bin
 └── textures/brick-albedo
asset-manifest.json       # Mapping file
```

### AssetTool CLI

```bash
dotnet pat manifest.json init           # Init manifest
dotnet pat manifest.json gui            # GUI tool
dotnet pat manifest.json clean          # Clean ContentBin
dotnet pat manifest.json add <files>    # Add files to manifest
dotnet pat manifest.json add .          # Add all files
dotnet pat manifest.json rem <files>    # Remove files
dotnet pat manifest.json list           # List manifest
dotnet pat manifest.json build          # Build all assets
dotnet pat manifest.json auto           # Watch & rebuild
```

The GUI provides file browser, asset preview, per-asset build options, error display (light/dark theme, resizable panels, Ctrl+MouseWheel zoom).

**Supported input formats:**
- **Models:** FBX, OBJ, GLTF, 3DS, DAE, BLEND → `.bin` (static meshes, skeleton animation, UVs, normals, tangents, bone weights)
- **Textures:** PNG, JPG, BMP, TGA → `.bin` (BC1/BC3/BC5/RGBA8 compression)
- **Shaders:** `.glsl`, `.vert`, `.frag` → copied as-is (compiled & verified at build time)

**Model load options:**

| Option | Default | Description |
|---|---|---|
| `IsAnimated` | `false` | Extract animations |
| `Extract Textures` | `true` | Extract embedded textures |
| `Assimp Flags` | per-option | Post-processing flags |

**Texture load options:**

| Option | Default | Description |
|---|---|---|
| `Generate MipMaps` | `true` | Generate mipmaps |
| `Compression` | `BC3` | Target compression |
| `Wrap Horizontal/Vertical` | `repeat` | Wrap mode |
| `Min/Mag filter` | `Linear Mipmap Linear` / `Linear` | Interpolation |

---

## Utilities

### Math Helpers

**Constants:** `MathHelper.Pi`, `TwoPi`, `PiOver2`, `PiOver4`

**float extensions:**
```csharp
float rads = 90f.ToRad();
float degs = MathF.PI.ToDeg();
float lerp = 0.5f.Lerp(0f, 1f);
float wrapped = 400f.WrapAngle(360f);
```

**Vector2:** `.ToNum()`, `.ToFloatArray()`, `.To2Df()`, `.To2Di()`

**Vector3:** `.ToFloatArray()`, `.ToStr()`, `.ToStrF2()`, `.ToStrInt()`, `.Normalize()`

**Vector4:** `.ToStrF2()`, `.ToStrInt()`

**Matrix4x4:**
```csharp
matrix.Invert();
matrix.InverseTranspose();
matrix.Transpose();
matrix.ToFloatArray();
matrix.ToStr();
Matrix4x4 rot = Matrix4x4.RotationMxFromYawPitchRoll(yaw, pitch, roll);
```

**Quaternion:**
```csharp
Quaternion q = Quaternion.RotationFromYawPitchRoll(yaw, pitch, roll);
q.ExtractYawPitchRoll(out float yaw, out float pitch, out float roll);
```

**String:** `"mixamorig:Hips".TrimBoneName()` → `"Hips"` (removes Mixamo prefixes)

**List:** `vertexPositions.ToFloatArrayList()` flattens `[v1, v2, v3]` → `[v1.X, v1.Y, v1.Z, ...]`

### Logging

Two mechanisms: `Log` (file-based) and `ErrorListWindow` (in-game overlay).

**Log configuration:**

```csharp
Log.Enabled = true;
Log.Verbose = true;
Log.ConsoleWrite = true;
Log.Date = true;
Log.Time = true;
```

**Log levels:** `Log.Info(...)`, `Log.Warn(...)`, `Log.Error(...)`, `Log.Debug(...)`, `Log.Exception(msg, ex)`

**Clear:** `Log.ClearLog()`

**Thread safety:** Writes protected by `lock`. On unhandled exception, `PhoenixGame.Run()` auto-enables all flags and writes to log.

**Log format:** `[2026-05-10 14:32:01] LEVEL: message`

---

## API Reference Summary

| Namespace | Key Classes |
|---|---|
| `Phoenix.Framework` | `PhoenixGame`, `Graphics`, `UI`, `Gizmos`, `InputManager` |
| `Phoenix.Framework.Cameras` | `Camera`, `BaseCamera`, `MouseCamera`, `FreeCamera` |
| `Phoenix.Framework.Rendering` | `ShaderHelper`, `ShaderUniform<T>`, `ShaderTextureUniform`, `GLTexture` |
| `Phoenix.Framework.Rendering.RT` | `RenderTarget` (fluent builder via `RTManager`) |
| `Phoenix.Framework.Audio` | `SoundManager`, `SoundClip`, `SoundInstance`, `IAudioDecoder` |
| `Phoenix.Framework.Collisions` | `Ray`, `BoundingSphere`, `AxisAlignedBoundingBox`, `OrientedBoundingBox`, `BoundingFrustum`, `BoundingCylinder`, `PlaneHelper` |
| `Phoenix.Framework.AssetImport` | `AssetLoader`, `Model`, `AnimatedModel`, `Animation` |
| `Phoenix.Framework.Utilities` | `Log`, `ErrorListWindow`, `MathHelper` |
