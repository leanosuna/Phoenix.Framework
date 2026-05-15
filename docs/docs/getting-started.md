# Getting Started

This guide walks you through setting up a Phoenix project from scratch.

## Prerequisites

- **.NET 8.0 SDK** or later
- A system with an OpenGL-compatible GPU and drivers

## Create a New Project

```bash
dotnet new console -n MyPhoenixGame
cd MyPhoenixGame
dotnet add package Silk.NET
dotnet add package Silk.NET.Input
dotnet add package Silk.NET.OpenGL
dotnet add package Silk.NET.OpenGL.Extensions.ImGui
dotnet add package Assimp.Unofficial
dotnet add package OpenTK.Audio.OpenAL   # or use the bundled OpenAL bindings
dotnet add package SixLabors.ImageSharp
```

Add a reference to the Phoenix.Framework project or NuGet package:

```bash
dotnet add reference ..\Phoenix.Framework\Phoenix.Framework\Phoenix.Framework.csproj
```

## Set Up the Game Class

Create `MyGame.cs`:

```csharp
using Phoenix.Framework;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public class MyGame : PhoenixGame
{
    protected override void Initialize()
    {
        // Create a free-look camera
        Camera = new FreeCamera(
            this,
            new Vector3(0, 5, -10),
            0, -MathF.PI / 4, MathF.PI / 4,
            0.1f, 1000f,
            WindowWidth / (float)WindowHeight
        );
        Camera.SetMoveKeys(Key.W, Key.S, Key.A, Key.D, Key.E, Key.Q, Key.LeftShift, 15f);

        // Enable depth testing and backface culling
        Graphics.SetDepthTest(true);
        Graphics.SetFaceCulling(true);
        Graphics.SetClearColor(new Vector4(0.1f, 0.15f, 0.2f, 1f));
    }

    protected override void Update(double dt)
    {
        Camera.Update((float)dt);
    }

    protected override void Render(double dt)
    {
        // Draw your scene here
        // Shaders receive View/Projection via CommonUBO at binding point 0
    }

    protected override void RenderUI()
    {
        // ImGui overlay rendered on top of the scene
    }
}
```

## Entry Point

```csharp
public static class Program
{
    public static void Main()
    {
        using var game = new MyGame();
        game.Run();
    }
}
```

## Run the Game

```bash
dotnet run
```

A window opens with a black background. The camera is positioned at `(0, 5, -10)` looking toward the origin. Press **F11** to toggle render halt (pause rendering while keeping the game loop running).

## Next Steps

- [Core Systems](core/game.md) — understand the game loop lifecycle
- [Graphics & Rendering](core/graphics.md) — render targets, GL state
- [Shaders](rendering/shaders.md) — write GLSL shaders and set uniforms
- [Models & Animation](rendering/models.md) — load and animate 3D models
- [Asset Pipeline](asset-pipeline/loading.md) — prepare your `Content/` folder
