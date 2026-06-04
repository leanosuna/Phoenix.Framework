# Shaders

The [AssetTool](../asset-pipeline/asset-tool.md) compiles and verifies Shaders and copy them to the ContentBin directory.  
Interacting with shaders is done via ShaderHelper classes.

## ShaderHelper class

On sucessfull build, the [AssetTool](../asset-pipeline/asset-tool.md) generates a child class of ShaderHelper combining Shader + name of your shader  
You're able to customize the namespace on the generated class in the [asset manifest](../asset-pipeline/loading.md#assetmanifest).

Both the parent, and any child class generated are declared ```partial``` so you are able to extend them as you see fit.

The API of this abstract class includes:
```csharp
public void Use() // Set this shader as the current OpenGL program
        
public void AttachUBO(
    uint bufferHandle, 
    string uniformBlockName, 
    uint binding = 0) //Used for Uniform Buffer Objects
        
public void Dispose() 
        
```
ShaderUniform Properties are added to the child class for each uniform found on your shader at compile time.  
With this approach, uniforms optimized out of shaders don't get generated, which causes C# compilation to fail
when trying to access these properties preventing runtime uniform not found errors.

## ShaderUniform <T\>

This class allows type checking of a shader uniform using a common Set(T value)  


## ShaderTextureUniform

This class allows binding a texture and using it in a shader


## Example ShaderHelper for a BasicModel.vert and BasicModel.frag shader pair.
```csharp
using System.Numerics;
using Phoenix.Framework.Rendering.Shaders;
using Phoenix.Framework.AssetImport;

namespace Phoenix.Framework.ShaderHelpers
{
	public partial class ShaderBasicModel : ShaderHelper
	{
		public ShaderUniform<Vector3> CameraPosition {get; private set;}
		public ShaderUniform<float> KA {get; private set;}
		public ShaderUniform<float> KD {get; private set;}
		public ShaderUniform<float> KS {get; private set;}
		public ShaderUniform<Vector3> LightColor {get; private set;}
		public ShaderUniform<Vector3> LightPosition {get; private set;}
		public ShaderTextureUniform TexColor {get; private set;}
		public ShaderUniform<Matrix4x4> World {get; private set;}

		public ShaderBasicModel()
		{
			_shader = AssetLoader.LoadShader("Shaders/basicModel/basicModel");

			CameraPosition = new ShaderUniform<Vector3>(_shader, "CameraPosition");
			KA = new ShaderUniform<float>(_shader, "KA");
			KD = new ShaderUniform<float>(_shader, "KD");
			KS = new ShaderUniform<float>(_shader, "KS");
			LightColor = new ShaderUniform<Vector3>(_shader, "LightColor");
			LightPosition = new ShaderUniform<Vector3>(_shader, "LightPosition");
			TexColor = new ShaderTextureUniform(_shader, "TexColor", 0);
			World = new ShaderUniform<Matrix4x4>(_shader, "World");
		}
	}
}
```




## See Also

- [CommonUBO](../core/graphics.md#commonubo) — shared uniform buffer
- [Models & Animation](models.md) — models pass bone matrices to shaders
- [Gizmos](gizmos.md) — gizmos use their own internal shader
