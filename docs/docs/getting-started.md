# Getting started

This guide walks you through setting up a Phoenix Framework v2 project from scratch.

## Prerequisites

- **.NET 10 SDK** or later.
- A GPU with Vulkan 1.3 support and current drivers (or MoltenVK on macOS).

## Create a new project

### 1. Solution and project structure

```bash
mkdir MyPhoenixGame
cd MyPhoenixGame
dotnet new sln -n MyPhoenixGame
dotnet new console -n MyPhoenixGame -o MyPhoenixGame
dotnet sln add MyPhoenixGame/MyPhoenixGame.csproj
```

### 2. Install the framework and local asset tool

```bash
cd MyPhoenixGame
dotnet add package Phoenix.Framework
dotnet new tool-manifest
dotnet tool install Phoenix.AssetTool
mkdir Content
```

### 3. Initialize an asset manifest

```bash
dotnet pat Content/asset-manifest.json init
```

### 4. Configure your csproj file

Include the manifest and binary bundles in output builds:

```xml
<ItemGroup>
    <None Include="Content\**" CopyToOutputDirectory="PreserveNewest" />
    <None Include=".contentbin\**" CopyToOutputDirectory="PreserveNewest" />
    <None Include="Content\asset-manifest.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Add an MSBuild target to build assets automatically before compiling:

```xml
<Target Name="Phoenix-AssetTool-Build" BeforeTargets="BeforeBuild">
    <Exec Command="dotnet pat Content/asset-manifest.json build" StandardOutputImportance="high" />
</Target>
```

---

## Understanding the PhoenixGame lifecycle

`PhoenixGame` coordinates internal subsystems automatically. Derived games implement the following methods:

### Required methods

- `Initialize()`: Subsystems and Vulkan graphics are ready. Load assets and configure scenes here.
- `Update(double deltaTime)`: Fires every frame. Game logic and physics updates belong here.
- `Render(RenderContext ctx, double deltaTime)`: Fires every frame. Record rendering commands into the provided `RenderContext`.

### Optional methods

- `RenderUI()`: Fires after 3D scene rendering to draw ImGui windows and user interface overlays.
- `OnWindowResize(Vector2 size)`: Fires when the window size changes.
- `OnClose()`: Fires during shutdown before releasing framework resources.
- `OnModelReloaded(ModelReloadEventArgs e)`: Fires when a 3D model asset is modified and recompiled by the asset server.
- `OnTextureReloaded(TextureReloadEventArgs e)`: Fires when a texture asset is updated.
- `OnShaderReloaded(ShaderReloadEventArgs e)`: Fires when a shader reloads.

---

## Set up the game class

Create `Game.cs`:

```csharp
using Phoenix.Framework;
using Phoenix.Framework.Cameras;
using Phoenix.Framework.Maths;
using Phoenix.Framework.Rendering;
using System.Numerics;

namespace MyPhoenixGame;

public class Game : PhoenixGame
{
    private Matrix4x4 _cubeWorld = Matrix4x4.Identity;

    public Game(string[]? args) : base(args)
    {
    }

    protected override void Initialize()
    {
        var cam = new FreeCamera(
            game: this,
            position: new Vector3(0, 0, -10),
            yaw: MathHelper.PiOver2,
            pitch: 0f,
            fov: MathHelper.PiOver2,
            nearPlane: 0.1f,
            farPlane: 1000f,
            aspectRatio: WindowWidth / (float)WindowHeight
        );
        cam.MouseAim = true;

        Camera = cam;
        Gizmos.Enabled = true;
    }

    protected override void Update(double deltaTime)
    {
        var t = (float)Metrics.Time;
        ((FreeCamera)Camera!).Update(deltaTime);

        _cubeWorld = Matrix4x4.CreateScale(3f)
            * MathHelper.RotationMxFromYawPitchRoll(t, MathF.Sin(t), MathF.Cos(t));
    }

    protected override void Render(RenderContext ctx, double deltaTime)
    {
        using var pass = ctx.BeginPass(new Vector4(0.08f, 0.08f, 0.12f, 1.0f));

        Gizmos.AddCube(_cubeWorld, Vector3.One);
    }

    protected override void RenderUI()
    {
        UI.DrawCenteredText(
            "Phoenix Framework v2 running on Vulkan 1.3",
            position: new Vector2(WindowWidth / 2f, 20),
            color: Vector4.One,
            size: 20
        );
    }

    protected override void OnWindowResize(Vector2 size)
    {
        if (Camera is FreeCamera cam)
            cam.AspectRatio = size.X / size.Y;
    }
}
```

---

## Entry point

Create `Program.cs`:

```csharp
namespace MyPhoenixGame;

public static class Program
{
    public static void Main(string[] args)
    {
        using var game = new Game(args);
        game.Run();
    }
}
```

---

## Run the game

```bash
dotnet run
```

A window opens with a dark background. The free camera sits at `(0, 0, -10)` looking toward the origin, and a wireframe cube rendered by Gizmos spins in the center.

Pass command-line arguments to change window properties or enable validation layers:

```bash
dotnet run -- --width 1280 --height 720 --validation
```

---

## Next steps

- [Game loop](core/game.md): Lifecycle methods and CLI arguments
- [Graphics and rendering](core/graphics.md): Vulkan device, swapchain, and dynamic rendering passes
- [Shaders and pipelines](rendering/shaders.md): SPIR-V compilation and push constants
- [Textures and bindless](rendering/textures.md): Bindless textures and samplers
- [Models and indirect rendering](rendering/models.md): Loading models and Multi-Draw Indirect
- [Asset pipeline](asset-pipeline/loading.md): Managing assets with the `pat` tool
