# Getting Started

This guide walks you through setting up a Phoenix project from scratch.

## Prerequisites

- **.NET 10 SDK** or later
- A system with an OpenGL-compatible GPU and drivers

## Create a New Project 

### First, your solution and project structure
```bash
mkdir MyPhoenixGame
cd MyPhoenixGame
dotnet new sln -n MyPhoenixGame
dotnet new console -n MyPhoenixGame -o MyPhoenixGame

dotnet sln MyPhoenixGame.slnx add MyPhoenixGame/MyPhoenixGame.csproj
```
### Now, install the framework and the asset tool.
```bash
cd MyPhoenixGame
dotnet add package Phoenix.Framework 
dotnet tool install Phoenix.AssetTool
mkdir Content
```

## Using the Phoenix Asset Tool, (pat) initialize an asset manifest
```bash
dotnet pat Content/asset-manifest.json init
```

Make sure the manifest file and the content files are included on build
```xml
<ItemGroup>
    <None Include="Content\ContentBin\**" CopyToOutputDirectory="PreserveNewest" />
    <None Include="Content\asset-manifest.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```
## Understanding the PhoenixGame lifecycle

Most systems are internally handled, the game class provides the following methods:

### Required
- ```Initialize()```: Internal systems ready. Load your assets and configurations here.
- ```Update(double deltaTime)```: Fires each frame. Game logic goes here.
- ```Render(double deltaTime)```: Fires each frame. Rendering goes here.
### Optional
- ```InitialLoadScreen()```: Fires the first frame, allows you to customize the "Loading game assets" screen before Initialize() 
- ```OnWindowResize(Vector2 size)```: Fires when resizing the game window, allows you to handle affected objects.
- ```OnClose()```: Fires when closing the game, allows you to handle custom object disposal.


## Set Up the Game Class

Create `Game.cs`:

```csharp
using Phoenix.Framework;
using Phoenix.Framework.Cameras;
using Phoenix.Framework.Rendering;
using System.Numerics;

public class Game : PhoenixGame
{
    Matrix4x4 _cubeWorld = Matrix4x4.Identity;

    protected override void InitialLoadScreen()
    {
        UI.DrawText("Custom startup screen", 
            position: Vector2.Zero, 
            color: Vector4.One, 
            size: 30);
    }
    protected override void Initialize()
    {
        // Asset loading ...
        var cam =  new FreeCamera(
            game: this,
            position: new Vector3(0, 0, -10),
            yaw: MathHelper.PiOver2,
            pitch: 0f,
            fov: MathHelper.PiOver2,
            nearPlane: 1f, 
            farPlane: 1000f,
            aspectRatio: WindowWidth / (float)WindowHeight
        );
        cam.MouseAim = true;

        Camera = cam;

        Gizmos.Enabled = true;

    }
                
    protected override void Update(double deltaTime)
    {
        // Game logic ...
        var t = (float)Graphics.Time;
        ((FreeCamera)Camera).Update(deltaTime);
        _cubeWorld = Matrix4x4.CreateScale(5f)
            * MathHelper.RotationMxFromYawPitchRoll(t, MathF.Sin(t), MathF.Cos(t));
    }

    protected override void Render(double deltaTime)
    {
        // Render logic ...
        Graphics.SetClearColor(new Vector4(0, 0, 0, 1));
        Graphics.ClearRenderTarget();

        Gizmos.AddCube(_cubeWorld, Vector3.One);
    }

    protected override void RenderUI()
    {
        // UI pass...
        UI.DrawCenteredText("This text is always pixel perfect!", 
            position: new Vector2(WindowWidth/2, 10),
            color: Vector4.One,
            size: 20);
    }

    protected override void OnWindowResize(Vector2 size)
    {
        // Something needs resizing...
    }

    protected override void OnClose()
    {
        // Something needs disposing...
    }
}
```

## Entry Point

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

## Run the Game

```bash
dotnet run
```

A window opens with a black background, the camera is positioned at `(0, 0, -10)` looking toward the origin. 
A wireframe cube from Gizmos is rendered spinning around in the center of the screen.

TODO: get this out of here Press **F11** to toggle render halt (pause rendering while keeping the game loop running).

## Next Steps

- [Core Systems](core/game.md) — understand the game loop lifecycle
- [Graphics & Rendering](core/graphics.md) — render targets, GL state
- [Shaders](rendering/shaders.md) — write GLSL shaders and set uniforms
- [Models & Animation](rendering/models.md) — load and animate 3D models
- [Asset Pipeline](asset-pipeline/loading.md) — prepare your `Content/` folder
